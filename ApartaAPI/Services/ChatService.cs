using ApartaAPI.Data;
using ApartaAPI.DTOs.Chat;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Services
{
    public class ChatService : IChatService
    {
        private readonly ApartaDbContext _context; // Dùng cho logic nghiệp vụ ngoài Chat
        private readonly IInteractionRepository _interactionRepo;
        private readonly IMessageRepository _messageRepo;
        private readonly IMapper _mapper; // [CẬP NHẬT] Inject Mapper

        public ChatService(
            ApartaDbContext context,
            IInteractionRepository interactionRepo,
            IMessageRepository messageRepo, IMapper mapper)
        {
            _context = context;
            _interactionRepo = interactionRepo;
            _messageRepo = messageRepo;
            _mapper = mapper;
        }

        // ===================================================================
        // LOGIC 1: TẠO HOẶC TÌM KIẾM CUỘC HỘI THOẠI (AD-HOC/THAY THẾ)
        // Đây là logic được sử dụng khi Frontend chọn một Partner cụ thể.
        // ===================================================================
        public async Task<InitiateInteractionDto> CreateAdHocInteractionAsync(string currentUserId, string partnerId)
        {
            if (currentUserId == partnerId)
            {
                throw new InvalidOperationException("Không thể tạo cuộc hội thoại với chính mình.");
            }

            // 1. Kiểm tra Interaction đã tồn tại chưa
            var existingInteraction = await _interactionRepo.GetInteractionByParticipantsAsync(currentUserId, partnerId);
            if (existingInteraction != null)
            {
                var partner = existingInteraction.ResidentId == currentUserId ? existingInteraction.Staff : existingInteraction.Resident;
                return new InitiateInteractionDto
                {
                    InteractionId = existingInteraction.InteractionId,
                    PartnerId = partner.UserId,
                    PartnerName = partner.Name
                };
            }

            // 2. Lấy thông tin cả hai bên (đảm bảo tồn tại và có Role)
            var sender = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId);
            var receiver = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == partnerId);

            if (sender == null || receiver == null)
            {
                throw new ArgumentException("Một trong các người dùng không tồn tại.");
            }

            // 3. Xác định ai là Resident và Staff/Admin (Bảo mật: Chỉ cho phép Resident chat với Staff)
            string residentId, staffId;
            var senderRole = sender.Role.RoleName.ToLower();
            var receiverRole = receiver.Role.RoleName.ToLower();

            bool isSenderStaff = senderRole.Contains("staff") || senderRole.Contains("manager") || senderRole == "admin";
            bool isReceiverStaff = receiverRole.Contains("staff") || receiverRole.Contains("manager") || receiverRole == "admin";

            if (senderRole == "resident" && isReceiverStaff)
            {
                residentId = sender.UserId;
                staffId = receiver.UserId;
            }
            else if (receiverRole == "resident" && isSenderStaff)
            {
                residentId = receiver.UserId;
                staffId = sender.UserId;
            }
            else
            {
                throw new InvalidOperationException("Chỉ có thể tạo cuộc hội thoại giữa Resident và Staff/Admin.");
            }

            // 4. Tạo Interaction mới
            var newInteraction = new Interaction
            {
                ResidentId = residentId,
                StaffId = staffId
            };
            var created = await _interactionRepo.AddAsync(newInteraction);

            return new InitiateInteractionDto
            {
                InteractionId = created.InteractionId,
                PartnerId = partnerId,
                PartnerName = receiver.Name
            };
        }

        // ===================================================================
        // LOGIC 2: LẤY DANH SÁCH CUỘC HỘI THOẠI (CỘT TRÁI)
        // ===================================================================
        public async Task<IEnumerable<InteractionListDto>> GetInteractionListAsync(string currentUserId)
        {
            var interactions = await _interactionRepo.GetUserInteractionsWithMessagesAsync(currentUserId);
            var results = new List<InteractionListDto>();

            foreach (var i in interactions)
            {
                var partner = i.ResidentId == currentUserId ? i.Staff : i.Resident;
                var lastMessage = i.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();

                // Lấy số lượng tin nhắn chưa đọc
                var unreadCount = await _messageRepo.GetUnreadCountForInteractionAsync(i.InteractionId, currentUserId);

                results.Add(new InteractionListDto
                {
                    InteractionId = i.InteractionId,
                    PartnerId = partner.UserId,
                    PartnerName = partner.Name,
                    PartnerAvatarUrl = partner.AvatarUrl,
                    LastMessageContent = lastMessage?.Content,
                    LastMessageSentAt = lastMessage?.SentAt,
                    UnreadCount = unreadCount
                });
            }

            return results;
        }

        // ===================================================================
        // LOGIC 3: LẤY LỊCH SỬ TIN NHẮN (LOAD MORE)
        // ===================================================================
        public async Task<PagedList<MessageDetailDto>> GetMessageHistoryAsync(
            string interactionId,
            string currentUserId,
            ChatQueryParameters queryParams)
        {
            // 1. Kiểm tra quyền truy cập (Bảo mật)
            var interaction = await _context.Interactions.FindAsync(interactionId);
            if (interaction == null || (interaction.ResidentId != currentUserId && interaction.StaffId != currentUserId))
            {
                throw new UnauthorizedAccessException("Không có quyền truy cập vào cuộc hội thoại này.");
            }

            // 2. Đánh dấu đã đọc (Giữ nguyên logic cũ)
            if (queryParams.PageNumber == 1)
            {
                await _messageRepo.MarkAsReadAsync(interactionId, currentUserId);
            }

            // 3. Xây dựng Query (Chưa execute vào DB)
            var query = _context.Messages.AsNoTracking()
                .Where(m => m.InteractionId == interactionId)
                .OrderByDescending(m => m.SentAt); // Lấy tin mới nhất trước

            // 4. Đếm tổng số (Query 1)
            var totalCount = await query.CountAsync();

            // 5. Phân trang & Map DTO trực tiếp (Query 2)
            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .OrderBy(m => m.SentAt) // Đảo ngược lại để hiển thị từ cũ -> mới trên UI
                .ProjectTo<MessageDetailDto>(_mapper.ConfigurationProvider) // Dùng AutoMapper
                .ToListAsync();

            // 6. Trả về PagedList
            return new PagedList<MessageDetailDto>(
                items,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        // ===================================================================
        // LOGIC 4: GỬI TIN NHẮN
        // ===================================================================
        public async Task<(Message? Message, string? ReceiverId)> SendMessageAsync(
            string currentUserId,
            SendMessageDto messageDto)
        {
            var interaction = await _interactionRepo.GetByIdAsync(messageDto.InteractionId);

            if (interaction == null || (interaction.ResidentId != currentUserId && interaction.StaffId != currentUserId))
            {
                return (null, null); // Interaction không hợp lệ hoặc không có quyền
            }

            var newMessage = new Message
            {
                InteractionId = messageDto.InteractionId,
                SenderId = currentUserId,
                Content = messageDto.Content,
                IsRead = false
            };

            var addedMessage = await _messageRepo.AddMessageAsync(newMessage);

            // Xác định người nhận để gửi SignalR
            var receiverId = interaction.ResidentId == currentUserId
                ? interaction.StaffId
                : interaction.ResidentId;

            return (addedMessage, receiverId);
        }

        // ===================================================================
        // LOGIC 5: TÌM KIẾM ĐỐI TÁC TIỀM NĂNG CHO COMBOBOX
        // ===================================================================
        public async Task<IEnumerable<PartnerDto>> SearchPotentialPartnersAsync(string currentUserId)
        {
            // 1. Lấy thông tin cơ bản của User đang đăng nhập
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Apartment).ThenInclude(a => a!.Building)
                .Include(u => u.StaffBuildingAssignmentUsers).ThenInclude(sba => sba.Building)
                .FirstOrDefaultAsync(u => u.UserId == currentUserId);

            if (currentUser == null)
            {
                return Enumerable.Empty<PartnerDto>();
            }

            var userRoleName = currentUser.Role.RoleName.ToLower();

            // 2. Xác định Building ID liên quan đến người dùng hiện tại
            List<string> buildingIds = new List<string>();

            if (userRoleName == "resident")
            {
                // Resident (Giả định gán vào Apartment) -> Lấy Building ID của Apartment đó
                if (currentUser.Apartment?.BuildingId != null)
                {
                    buildingIds.Add(currentUser.Apartment.BuildingId);
                }
            }
            else if (userRoleName.Contains("staff") || userRoleName == "admin")
            {
                // Staff/Admin -> Lấy tất cả Building ID mà họ được gán
                buildingIds = currentUser.StaffBuildingAssignmentUsers
                    .Where(sba => sba.IsActive)
                    .Select(sba => sba.BuildingId)
                    .ToList();
            }

            if (!buildingIds.Any())
            {
                // Nếu không có Building ID nào được tìm thấy (ví dụ: Staff chưa được gán)
                return Enumerable.Empty<PartnerDto>();
            }

            // 3. Truy vấn các đối tác tiềm năng dựa trên Building ID(s)
            IQueryable<User> partnersQuery;

            if (userRoleName == "resident")
            {
                // Resident cần tìm: Tất cả Staff được gán cho Building đó
                partnersQuery = _context.Users
                    .Where(u => u.StaffBuildingAssignmentUsers.Any(sba => buildingIds.Contains(sba.BuildingId) && sba.IsActive)
                                && u.UserId != currentUserId);
            }
            else // Staff / Admin cần tìm: Tất cả Residents trong các Building họ quản lý
            {
                // Tìm tất cả Users là Resident và có Apartment thuộc Building quản lý
                partnersQuery = _context.Users
                    .Where(u => u.Apartment != null && buildingIds.Contains(u.Apartment.BuildingId)
                                && u.Role.RoleName.ToLower() == "resident"
                                && u.UserId != currentUserId);
            }

            // 4. Thực thi truy vấn và ánh xạ sang DTO
            var partners = await partnersQuery
                .Select(u => new PartnerDto
                {
                    UserId = u.UserId,
                    FullName = u.Name,
                    AvatarUrl = u.AvatarUrl,
                    Role = u.Role.RoleName,
                    ApartmentCode = u.Apartment != null ? u.Apartment.Code : null
                })
                .Distinct() // Loại bỏ trùng lặp nếu có
                .OrderBy(p => p.FullName)
                .ToListAsync();

            return partners;
        }
    }
}

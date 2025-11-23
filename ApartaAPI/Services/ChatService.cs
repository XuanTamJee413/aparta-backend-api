using ApartaAPI.Data;
using ApartaAPI.DTOs.Chat;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Services
{
    public class ChatService : IChatService
    {
        private readonly ApartaDbContext _context; // Dùng cho logic nghiệp vụ ngoài Chat
        private readonly IInteractionRepository _interactionRepo;
        private readonly IMessageRepository _messageRepo;

        public ChatService(
            ApartaDbContext context,
            IInteractionRepository interactionRepo,
            IMessageRepository messageRepo)
        {
            _context = context;
            _interactionRepo = interactionRepo;
            _messageRepo = messageRepo;
        }

        // ===================================================================
        // LOGIC 1: KHỞI TẠO HOẶC TÌM KIẾM CUỘC HỘI THOẠI
        // ===================================================================
        public async Task<InitiateInteractionDto> InitiateOrGetInteractionAsync(string currentUserId)
        {
            // 1. Giả định User hiện tại là Resident (ResidentId)
            var residentUser = await _context.Users
                .Include(u => u.Apartment)
                .FirstOrDefaultAsync(u => u.UserId == currentUserId);

            if (residentUser?.ApartmentId == null || residentUser.Apartment == null)
            {
                // Xử lý khi User không phải Resident hoặc không gán với Apartment
                // Nếu là Staff, cần tìm StaffID tương ứng với ResidentID
                // -> Để đơn giản, giả sử chỉ Resident khởi tạo chat với Staff
                throw new Exception("Người dùng không liên kết với căn hộ.");
            }

            var apartment = residentUser.Apartment;
            var buildingId = apartment.BuildingId;

            // 2. Tìm Staff phụ trách Building
            var assignment = await _context.StaffBuildingAssignments
                .Where(sba => sba.BuildingId == buildingId && sba.IsActive)
                .OrderBy(sba => sba.User.Role.RoleName) // Ưu tiên Staff có Role cao hơn (ví dụ: Admin)
                .Select(sba => sba.UserId)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                throw new Exception("Không tìm thấy Staff phụ trách cho tòa nhà này.");
            }

            var staffId = assignment;

            // 3. Kiểm tra Interaction đã tồn tại chưa
            var interaction = await _interactionRepo.GetInteractionByParticipantsAsync(currentUserId, staffId);

            if (interaction == null)
            {
                // 4. Tạo Interaction mới
                interaction = new Interaction
                {
                    ResidentId = residentUser.UserId,
                    StaffId = staffId
                };
                interaction = await _interactionRepo.AddAsync(interaction);
            }

            var partnerUser = await _context.Users.FindAsync(staffId);

            return new InitiateInteractionDto
            {
                InteractionId = interaction.InteractionId,
                PartnerId = staffId,
                PartnerName = partnerUser?.Name ?? "Staff (Không rõ tên)"
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
        public async Task<IEnumerable<MessageDetailDto>> GetMessageHistoryAsync(
            string interactionId,
            string currentUserId,
            int pageNumber,
            int pageSize)
        {
            // Kiểm tra quyền truy cập (Người dùng có thuộc Interaction này không?)
            var interaction = await _context.Interactions.FindAsync(interactionId);
            if (interaction == null || (interaction.ResidentId != currentUserId && interaction.StaffId != currentUserId))
            {
                throw new UnauthorizedAccessException("Không có quyền truy cập vào cuộc hội thoại này.");
            }

            int skip = (pageNumber - 1) * pageSize;
            var messages = await _messageRepo.GetMessagesAsync(interactionId, skip, pageSize);

            // Đánh dấu là đã đọc (Mark as Read)
            if (pageNumber == 1) // Chỉ đánh dấu khi người dùng mở lần đầu (hoặc load trang đầu)
            {
                await _messageRepo.MarkAsReadAsync(interactionId, currentUserId);
            }

            // Chuyển đổi sang DTO
            return messages.Select(m => new MessageDetailDto
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                Content = m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead
            });
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
    }
}

using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.Utils.Helper;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserManagementRepository _userManagementRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<StaffBuildingAssignment> _sbaRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly IMapper _mapper; // [MỚI] Inject Mapper
        private readonly ApartaDbContext _context;
        private readonly IMailService _mailService;
        private readonly IConfiguration _configuration;
        public UserManagementService(
            IUserManagementRepository userManagementRepo,
            IRepository<User> userRepo,
            IRepository<StaffBuildingAssignment> sbaRepo,
            IRepository<Role> roleRepo,
            IMapper mapper,
            ApartaDbContext context, IMailService mailService,
            IConfiguration configuration)
        {
            _userManagementRepo = userManagementRepo;
            _userRepo = userRepo;
            _sbaRepo = sbaRepo;
            _roleRepo = roleRepo;
            _mapper = mapper;
            _context = context;
            _mailService = mailService;
            _configuration = configuration;
        }

        // Helper nội bộ xử lý logic chung
        private async Task<PagedList<UserAccountDto>> GetAccountsByRoleInternalAsync(UserQueryParams queryParams, List<string> roles)
        {
            // 1. Lấy dữ liệu phân trang từ Repo
            var pagedUsers = await _userManagementRepo.GetPagedUsersAsync(queryParams, roles);

            // 2. Dùng AutoMapper map sang DTO (nhanh và gọn hơn vòng lặp thủ công)
            var userDtos = _mapper.Map<List<UserAccountDto>>(pagedUsers.Items);

            // 3. Xử lý logic phụ (Map thêm BuildingCodes hoặc ApartmentCode)
            // Vì AutoMapper khó xử lý query async trong profile, ta xử lý ở đây
            foreach (var dto in userDtos)
            {
                // Vì DTO đã có UserId, ta dùng nó để query thêm info
                if (roles.Contains("resident"))
                {
                    // ApartmentCode đã được Map trong Profile nếu User.Apartment được Include
                    // Nếu Repo chưa Include Apartment thì cần check lại Repo
                }
                else
                {
                    // Lấy danh sách tòa nhà phụ trách cho Staff
                    var assignments = await _userManagementRepo.GetStaffAssignmentsAsync(dto.UserId);
                    dto.AssignedBuildingCodes = assignments.Select(a => a.Building.BuildingCode).ToList();
                }
            }

            return new PagedList<UserAccountDto>(userDtos, pagedUsers.TotalCount, pagedUsers.PageNumber, pagedUsers.PageSize);
        }

        public async Task<ApiResponse<PagedList<UserAccountDto>>> GetStaffAccountsAsync(UserQueryParams queryParams)
        {
            try
            {
                // [FIX LỖI CŨ] Chỉ lấy các role thực sự là nhân viên, LOẠI BỎ Admin/Manager
                var staffRoles = new List<string> { "staff", "operation_staff", "finance_staff", "maintenance_staff", "custom" };

                var result = await GetAccountsByRoleInternalAsync(queryParams, staffRoles);
                return ApiResponse<PagedList<UserAccountDto>>.Success(result);
            }
            catch (Exception ex)
            {
                // Log error here
                return ApiResponse<PagedList<UserAccountDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        public async Task<ApiResponse<PagedList<UserAccountDto>>> GetResidentAccountsAsync(UserQueryParams queryParams)
        {
            try
            {
                var residentRoles = new List<string> { "resident" };
                var result = await GetAccountsByRoleInternalAsync(queryParams, residentRoles);
                return ApiResponse<PagedList<UserAccountDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedList<UserAccountDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        public async Task<UserAccountDto> CreateStaffAccountAsync(StaffCreateDto createDto)
        {
            // 1. Validate
            var role = await _roleRepo.GetByIdAsync(createDto.RoleId);
            if (role == null) throw new ArgumentException("Role ID không hợp lệ.");

            if (await _userRepo.FirstOrDefaultAsync(u => u.Email == createDto.Email) != null)
                throw new InvalidOperationException("Email đã tồn tại.");

            if (await _userRepo.FirstOrDefaultAsync(u => u.Phone == createDto.Phone) != null)
                throw new InvalidOperationException("Số điện thoại đã tồn tại.");

            // 2. Map & Create
            var newUser = _mapper.Map<User>(createDto); // Dùng AutoMapper
            newUser.UserId = Guid.NewGuid().ToString();
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password);
            newUser.Status = "active";
            newUser.IsFirstLogin = true;
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            await _userRepo.AddAsync(newUser);
            if (!string.IsNullOrWhiteSpace(newUser.Email))
            {
                try
                {
                    var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";

                    // Sử dụng EmailTemplateHelper vừa tạo
                    var htmlMessage = EmailTemplateHelper.GetStaffWelcomeEmailTemplate(
                        newUser.Name,
                        newUser.Phone ?? "",
                        newUser.Email,
                        createDto.Password, // Gửi mật khẩu thô để họ đăng nhập lần đầu
                        frontendUrl
                    );

                    // Gọi MailService (đã được cấu hình SMTP trong Program.cs)
                    await _mailService.SendEmailAsync(
                        newUser.Email,
                        "Thông tin tài khoản Nhân viên - Aparta System",
                        htmlMessage
                    );
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không chặn luồng tạo user
                    Console.WriteLine($"[Warning] Không thể gửi email cho Staff mới: {ex.Message}");
                }
            }
            // 3. Return DTO
            // Tạo role list tạm để gọi hàm hiển thị kết quả chuẩn xác
            var roles = new List<string> { role.RoleName };
            var tempQuery = new UserQueryParams { PageNumber = 1, PageSize = 1, SearchTerm = newUser.Phone };
            var pagedResult = await GetAccountsByRoleInternalAsync(tempQuery, roles);

            return pagedResult.Items.FirstOrDefault() ?? _mapper.Map<UserAccountDto>(newUser);
        }

        public async Task<UserAccountDto> ToggleUserStatusAsync(string userId, StatusUpdateDto dto)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User không tồn tại.");

            var newStatus = dto.Status.Trim().ToLowerInvariant();
            if (newStatus != "active" && newStatus != "inactive")
                throw new ArgumentException("Trạng thái chỉ chấp nhận 'Active' hoặc 'Inactive'.");

            user.Status = newStatus;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);

            // Fetch lại để có role info cho việc map DTO trả về
            var updatedUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);

            // Re-use logic lấy DTO chuẩn
            var roles = new List<string> { updatedUser!.Role.RoleName };
            var tempQuery = new UserQueryParams { PageNumber = 1, PageSize = 1, SearchTerm = updatedUser.Phone };
            var pagedResult = await GetAccountsByRoleInternalAsync(tempQuery, roles);

            return pagedResult.Items.First();
        }

        public async System.Threading.Tasks.Task UpdateStaffAssignmentAsync(string staffId, AssignmentUpdateDto updateDto)
        {
            var staff = await _userRepo.GetByIdAsync(staffId);
            if (staff == null) throw new KeyNotFoundException($"Staff ID '{staffId}' không tồn tại.");

            // Logic transaction để đảm bảo toàn vẹn dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Xóa assignments cũ
                var oldAssignments = await _sbaRepo.FindAsync(sba => sba.UserId == staffId);
                foreach (var item in oldAssignments)
                {
                    await _sbaRepo.RemoveAsync(item);
                }

                // 2. Thêm assignments mới
                var now = DateTime.UtcNow;
                foreach (var buildingId in updateDto.BuildingIds)
                {
                    var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
                    if (building == null) throw new KeyNotFoundException($"Building ID '{buildingId}' không tồn tại.");

                    var newAssignment = new StaffBuildingAssignment
                    {
                        AssignmentId = Guid.NewGuid().ToString(),
                        UserId = staffId,
                        BuildingId = buildingId,
                        AssignmentStartDate = DateOnly.FromDateTime(now),
                        ScopeOfWork = updateDto.ScopeOfWork,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _sbaRepo.AddAsync(newAssignment);
                }

                await _sbaRepo.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
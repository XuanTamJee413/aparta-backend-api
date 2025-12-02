using ApartaAPI.Data;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.BackgroundJobs
{
    public class SubscriptionExpiryService : BackgroundService
    {
        private readonly ILogger<SubscriptionExpiryService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1); // Chạy mỗi ngày 1 lần

        public SubscriptionExpiryService(ILogger<SubscriptionExpiryService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscription Expiry Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckSubscriptions(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while checking subscriptions.");
                }

                // Đợi khoảng thời gian check tiếp theo
                await System.Threading.Tasks.Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Subscription Expiry Service is stopping.");
        }

        private async System.Threading.Tasks.Task CheckSubscriptions(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running subscription check at: {time}", DateTimeOffset.Now);

            // Tạo scope mới để lấy DbContext và Repositories
            using (var scope = _scopeFactory.CreateScope())
            {
                var subscriptionRepo = scope.ServiceProvider.GetRequiredService<IRepository<Subscription>>();
                var projectRepo = scope.ServiceProvider.GetRequiredService<IRepository<Project>>();

                // Các service bổ sung
                var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApartaDbContext>();
                var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var now = DateTime.UtcNow;
                var warningThreshold = now.AddDays(7); // Ngưỡng cảnh báo: 7 ngày trước khi hết hạn

                // ==================================================================
                // 1. XỬ LÝ CẢNH BÁO SẮP HẾT HẠN & GỬI EMAIL
                // ==================================================================
                var upcomingExpiries = await subscriptionRepo.FindAsync(s =>
                    s.Status == "Active" &&
                    s.ExpiredAt > now && // Vẫn còn hạn
                    s.ExpiredAt <= warningThreshold // Nhưng sắp hết hạn
                );

                if (upcomingExpiries.Any())
                {
                    // Lấy danh sách Project ID cần cảnh báo
                    var upcomingProjectIds = upcomingExpiries.Select(s => s.ProjectId).Distinct().ToList();

                    // Load thông tin Project để lấy ProjectCode/Name
                    var upcomingProjects = await projectRepo.FindAsync(p => upcomingProjectIds.Contains(p.ProjectId));

                    // [LOGIC MỚI] Tìm Email của Manager phụ trách các dự án này
                    // Logic: Project -> Building -> StaffBuildingAssignment -> User (Role = Manager)
                    var managerEmailsMap = await GetManagerEmailsForProjectsAsync(dbContext, upcomingProjectIds);

                    var frontendUrl = configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";

                    foreach (var sub in upcomingExpiries)
                    {
                        var project = upcomingProjects.FirstOrDefault(p => p.ProjectId == sub.ProjectId);
                        var projectIdentifier = project?.ProjectCode ?? project?.Name ?? sub.ProjectId;
                        var adminId = project?.AdminId ?? "N/A";

                        _logger.LogWarning($"Subscription Warning: Project '{projectIdentifier}' (Admin: {adminId}) is expiring soon on {sub.ExpiredAt.ToLocalTime()}.");

                        // Gửi mail cho các Manager liên quan
                        if (managerEmailsMap.ContainsKey(sub.ProjectId))
                        {
                            var managers = managerEmailsMap[sub.ProjectId];
                            foreach (var manager in managers)
                            {
                                try
                                {
                                    var emailBody = BuildExpiryWarningEmail(manager.Name, projectIdentifier, sub.ExpiredAt, frontendUrl);
                                    await mailService.SendEmailAsync(
                                        manager.Email,
                                        $"[Aparta] Cảnh báo hết hạn gói dịch vụ - {projectIdentifier}",
                                        emailBody
                                    );
                                    _logger.LogInformation($"Sent expiry warning email to {manager.Email} for project {projectIdentifier}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, ($"Failed to send email to {manager.Email}"));
                                }
                            }
                        }
                    }
                }

                // ==================================================================
                // 2. XỬ LÝ HẾT HẠN & DEACTIVATE PROJECT
                // ==================================================================
                var expiredSubscriptions = await subscriptionRepo.FindAsync(s =>
                    s.Status == "Active" &&
                    s.ExpiredAt <= now // Đã hết hạn
                );

                if (expiredSubscriptions.Any())
                {
                    // Update trạng thái các gói đã hết hạn
                    foreach (var sub in expiredSubscriptions)
                    {
                        sub.Status = "Expired";
                        sub.UpdatedAt = now;
                        await subscriptionRepo.UpdateAsync(sub);
                        _logger.LogInformation($"Subscription {sub.SubscriptionId} for Project {sub.ProjectId} has expired. Status set to Expired.");
                    }

                    // Lưu thay đổi trạng thái Subscription trước
                    await subscriptionRepo.SaveChangesAsync();

                    // Kiểm tra xem Project có cần bị Deactive không?
                    var distinctProjectIds = expiredSubscriptions.Select(s => s.ProjectId).Distinct().ToList();
                    var projectsToCheck = await projectRepo.FindAsync(p => distinctProjectIds.Contains(p.ProjectId) && p.IsActive == true);

                    foreach (var proj in projectsToCheck)
                    {
                        // Kiểm tra kỹ: Còn gói Active nào khác còn hạn không? (Logic cộng dồn)
                        bool hasOtherActiveSub = await subscriptionRepo.FirstOrDefaultAsync(s =>
                                                        s.ProjectId == proj.ProjectId &&
                                                        s.Status == "Active" &&
                                                        s.ExpiredAt > now) != null;

                        if (!hasOtherActiveSub)
                        {
                            // [QUAN TRỌNG] Gọi ProjectService để Deactivate
                            // Điều này sẽ kích hoạt logic: User -> Inactive, StaffAssignment -> Inactive
                            _logger.LogInformation($"Project {proj.ProjectId} has no active subscriptions left. Deactivating...");

                            var updateDto = new ProjectUpdateDto(
                                null, // Name
                                null, // Address
                                null, // Ward
                                null, // District
                                null, // City
                                null, // BankName
                                null, // BankAccountNumber
                                null, // BankAccountName
                                null, // PayOSClientId
                                null, // PayOSApiKey
                                null, // PayOSChecksumKey
                                false  // [FIXED] IsActive = false để tắt dự án
                            );

                            var result = await projectService.UpdateAsync(proj.ProjectId, updateDto);

                            if (result.Succeeded)
                            {
                                _logger.LogInformation($"Project {proj.ProjectId} deactivated successfully.");
                            }
                            else
                            {
                                _logger.LogError($"Failed to deactivate Project {proj.ProjectId}: {result.Message}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"Project {proj.ProjectId} still has other active subscriptions. Remaining Active.");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No subscriptions expired in this check.");
                }
            }
        }

        // Helper: Lấy danh sách Email và Tên của Manager theo ProjectId
        private async Task<Dictionary<string, List<(string Email, string Name)>>> GetManagerEmailsForProjectsAsync(
            ApartaDbContext context,
            List<string> projectIds)
        {
            // Query: Project -> Building -> StaffBuildingAssignment -> User (Role=Manager)
            var query = await context.StaffBuildingAssignments
                .Include(sba => sba.Building)
                .Include(sba => sba.User)
                .ThenInclude(u => u.Role)
                .Where(sba =>
                    projectIds.Contains(sba.Building.ProjectId) &&   // Thuộc các project sắp hết hạn
                    sba.IsActive &&                                  // Phân công đang active
                    sba.User.IsDeleted == false &&                   // User chưa bị xóa
                    sba.User.Status == "active" &&                   // User đang active
                    sba.User.Role.RoleName == "manager"              // Role là Manager
                )
                .Select(sba => new
                {
                    ProjectId = sba.Building.ProjectId,
                    UserEmail = sba.User.Email,
                    UserName = sba.User.Name
                })
                .ToListAsync();

            // Group lại theo ProjectId
            var result = new Dictionary<string, List<(string Email, string Name)>>();

            foreach (var item in query)
            {
                if (string.IsNullOrEmpty(item.UserEmail)) continue;

                if (!result.ContainsKey(item.ProjectId))
                {
                    result[item.ProjectId] = new List<(string Email, string Name)>();
                }

                // Tránh add trùng email cho 1 project (nếu manager quản lý nhiều tòa trong cùng 1 project)
                if (!result[item.ProjectId].Any(x => x.Email == item.UserEmail))
                {
                    result[item.ProjectId].Add((item.UserEmail, item.UserName));
                }
            }

            return result;
        }

        // Helper: Tạo nội dung Email HTML
        private string BuildExpiryWarningEmail(string managerName, string projectName, DateTime expiredAt, string frontendUrl)
        {
            // Convert expiredAt to Local Time string if needed
            var dateStr = expiredAt.ToLocalTime().ToString("dd/MM/yyyy");

            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                        <h2 style='color: #d9534f; text-align: center;'>⚠️ Cảnh báo hết hạn dịch vụ</h2>
                        
                        <p>Xin chào <strong>{managerName}</strong>,</p>
                        
                        <p>Hệ thống <strong>Aparta</strong> xin thông báo: Gói dịch vụ quản lý cho dự án <strong>{projectName}</strong> sắp hết hạn.</p>
                        
                        <div style='background-color: #f9f9f9; padding: 15px; margin: 20px 0; border-left: 4px solid #d9534f;'>
                            <p style='margin: 0;'><strong>Ngày hết hạn:</strong> {dateStr}</p>
                        </div>

                        <p>Vui lòng thực hiện gia hạn sớm để đảm bảo việc vận hành không bị gián đoạn. Nếu gói dịch vụ hết hạn, hệ thống sẽ tự động tạm dừng quyền truy cập của cư dân và nhân viên.</p>

                        <div style='text-align: center; margin-top: 30px;'>
                            <a href='{frontendUrl}/admin/subscription/list' 
                               style='background-color: #0275d8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold;'>
                                Gia hạn ngay
                            </a>
                        </div>

                        <p style='margin-top: 30px; font-size: 12px; color: #777; text-align: center;'>
                            Đây là email tự động, vui lòng không trả lời.<br>
                            © 2025 Aparta System
                        </p>
                    </div>
                </body>
                </html>";
        }
    }
}
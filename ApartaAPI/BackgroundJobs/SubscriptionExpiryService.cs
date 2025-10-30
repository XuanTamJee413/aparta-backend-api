using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;

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
                // var dbContext = scope.ServiceProvider.GetRequiredService<ApartaDbContext>();

                var now = DateTime.UtcNow;
                var warningThreshold = now.AddDays(7); // Ngưỡng cảnh báo: 7 ngày trước khi hết hạn

                // --- 1. Xử lý cảnh báo sắp hết hạn ---
                var upcomingExpiries = await subscriptionRepo.FindAsync(s =>
                    s.Status == "Active" &&
                    s.ExpiredAt > now && // Vẫn còn hạn
                    s.ExpiredAt <= warningThreshold // Nhưng sắp hết hạn
                );

                // Tải thông tin Project liên quan để lấy AdminId
                var upcomingProjectIds = upcomingExpiries.Select(s => s.ProjectId).Distinct().ToList();
                var upcomingProjects = await projectRepo.FindAsync(p => upcomingProjectIds.Contains(p.ProjectId));
                var projectAdminMap = upcomingProjects.ToDictionary(p => p.ProjectId, p => p.AdminId); // Tạo map để tra cứu AdminId

                foreach (var sub in upcomingExpiries)
                {
                    projectAdminMap.TryGetValue(sub.ProjectId, out var adminId);
                    // Log ra console (Bạn có thể thay bằng gửi thông báo thực tế)
                    _logger.LogWarning($"Subscription Warning: Project '{sub.ProjectId}' (Admin: {adminId ?? "N/A"}) is expiring soon on {sub.ExpiredAt.ToLocalTime()}.");
                }

                // --- 2. Xử lý hết hạn và Deactivate Project ---
                var expiredSubscriptions = await subscriptionRepo.FindAsync(s =>
                    s.Status == "Active" &&
                    s.ExpiredAt <= now // Đã hết hạn
                );

                if (expiredSubscriptions.Any())
                {
                    var expiredProjectIds = expiredSubscriptions.Select(s => s.ProjectId).Distinct().ToList();
                    var projectsToDeactivate = await projectRepo.FindAsync(p => expiredProjectIds.Contains(p.ProjectId) && p.IsActive == true);

                    bool changesMade = false;
                    foreach (var sub in expiredSubscriptions)
                    {
                        // Đổi trạng thái Subscription thành Expired
                        sub.Status = "Expired";
                        sub.UpdatedAt = now;
                        await subscriptionRepo.UpdateAsync(sub);
                        changesMade = true;
                        _logger.LogInformation($"Subscription {sub.SubscriptionId} for Project {sub.ProjectId} has expired. Status set to Expired.");
                    }

                    foreach (var proj in projectsToDeactivate)
                    {
                        // Chỉ deactive Project nếu không còn gói Active nào khác (đề phòng trường hợp hiếm)
                        bool hasOtherActiveSub = await subscriptionRepo.FirstOrDefaultAsync(s =>
                                                        s.ProjectId == proj.ProjectId &&
                                                        s.Status == "Active" &&
                                                        s.ExpiredAt > now) != null;

                        if (!hasOtherActiveSub)
                        {
                            proj.IsActive = false; // Xóa mềm Project
                            proj.UpdatedAt = now;
                            await projectRepo.UpdateAsync(proj);
                            changesMade = true;
                            _logger.LogInformation($"Project {proj.ProjectId} deactivated due to expired subscription.");
                        }
                        else
                        {
                            _logger.LogWarning($"Project {proj.ProjectId} has an expired subscription, but another active subscription exists. Project remains active.");
                        }
                    }

                    // Lưu tất cả thay đổi vào DB
                    if (changesMade)
                    {
                        await projectRepo.SaveChangesAsync();
                        _logger.LogInformation("Saved changes for expired subscriptions and deactivated projects.");
                    }
                }
                else
                {
                    _logger.LogInformation("No subscriptions expired in this check.");
                }
            }
        }
    }
}

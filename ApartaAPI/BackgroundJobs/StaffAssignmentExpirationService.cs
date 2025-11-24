using ApartaAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.BackgroundJobs
{
    public class StaffAssignmentExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StaffAssignmentExpirationService> _logger;

        public StaffAssignmentExpirationService(
            IServiceProvider serviceProvider,
            ILogger<StaffAssignmentExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Staff Assignment Expiration Service is starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndExpireAssignmentsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking expired assignments.");
                }

                // Chạy định kỳ mỗi 1 tiếng (hoặc 24h tùy bạn chỉnh)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CheckAndExpireAssignmentsAsync()
        {
            // Tạo scope mới vì BackgroundService là Singleton còn DbContext là Scoped
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApartaDbContext>();

                var today = DateOnly.FromDateTime(DateTime.UtcNow); // Dùng UtcNow cho chuẩn server

                // Lấy các phân công đang ACTIVE nhưng ngày kết thúc nhỏ hơn hôm nay
                var expiredAssignments = await context.StaffBuildingAssignments
                    .Where(x => x.IsActive == true &&
                                x.AssignmentEndDate != null &&
                                x.AssignmentEndDate < today)
                    .ToListAsync();

                if (expiredAssignments.Any())
                {
                    _logger.LogInformation($"Found {expiredAssignments.Count} expired staff assignments.");

                    foreach (var assignment in expiredAssignments)
                    {
                        assignment.IsActive = false;
                        assignment.UpdatedAt = DateTime.UtcNow;
                    }

                    await context.SaveChangesAsync();
                    _logger.LogInformation("Expired staff assignments have been deactivated successfully.");
                }
            }
        }
    }
}
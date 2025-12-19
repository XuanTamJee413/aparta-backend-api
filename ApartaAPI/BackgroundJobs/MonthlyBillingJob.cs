using ApartaAPI.Data;
using ApartaAPI.DTOs.Invoices;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApartaAPI.BackgroundJobs;

public class MonthlyBillingJob : BackgroundService
{
    private const string SystemUserId = "SYSTEM_JOB";
    private readonly ILogger<MonthlyBillingJob> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public MonthlyBillingJob(ILogger<MonthlyBillingJob> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    // Vòng lặp của dịch vụ nền, chạy liên tục cho đến khi bị hủy.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonthlyBillingJob is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetDelayUntilNextRun();
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogDebug("MonthlyBillingJob waiting {Delay} until next execution.", delay);
                    await Task.Delay(delay, stoppingToken);
                }

                await DoWork(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MonthlyBillingJob encountered an unexpected error.");
                // Wait a short time before retrying to avoid tight loop on failure
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("MonthlyBillingJob is stopping.");
    }

	// Chayj định kỳ 
    private async Task DoWork(CancellationToken stoppingToken)
    {
		var now = DateTime.Now;
		var today = now.Day; 
		var dayToTrigger = today;// ngày sau khi kết thúc  "reading window end"

        if (dayToTrigger <= 0)
        {
            _logger.LogInformation("MonthlyBillingJob skipped because dayToTrigger ({DayToTrigger}) is not positive.", dayToTrigger);
            return;
        }

		var billingPeriod = now.AddMonths(-1).ToString("yyyy-MM");
        //var billingPeriod = now.ToString("yyyy-MM");
        _logger.LogInformation("MonthlyBillingJob running for billing period {BillingPeriod} targeting reading window end day {DayToTrigger}.", billingPeriod, dayToTrigger);

        using var scope = _scopeFactory.CreateScope();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApartaDbContext>();

        var buildings = await dbContext.Buildings
            // Chỉ lấy các tòa nhà đang hoạt động và có ngày kết thúc cửa sổ bằng dayToTrigger
			.Where(b => b.IsActive && b.ReadingWindowEnd == dayToTrigger)
            .ToListAsync(stoppingToken);

        if (buildings.Count == 0)
        {
            _logger.LogInformation("MonthlyBillingJob found no buildings with reading window end matching day {DayToTrigger}.", dayToTrigger);
            return;
        }

        foreach (var building in buildings)
        {
            try
            {
				// Chuẩn bị request tạo hóa đơn cho tòa nhà theo kỳ đã xác định.
                var request = new GenerateInvoicesRequest
                {
                    BuildingId = building.BuildingId,
                    BillingPeriod = billingPeriod
                };

				// Gọi service tạo hóa đơn (chạy cho tất cả căn hộ/dịch vụ theo cấu hình nội bộ).
				var result = await invoiceService.GenerateInvoicesAsync(request, SystemUserId); // Truyền "SYSTEM_JOB" để audit

                if (result.Success)
                {
                    _logger.LogInformation(
                        "MonthlyBillingJob generated {Count} invoice items for building {BuildingId} ({BillingPeriod}).",
                        result.ProcessedCount,
                        building.BuildingId,
                        billingPeriod);

                    // Gửi email thông báo cho từng resident sau khi tạo invoice thành công
                    var emailResult = await invoiceService.SendInvoiceEmailsToResidentsAsync(building.BuildingId, billingPeriod);
                    _logger.LogInformation(
                        "MonthlyBillingJob: Sent {SentCount} emails, {FailedCount} failed for building {BuildingId} ({BillingPeriod}).",
                        emailResult.SentCount,
                        emailResult.FailedCount,
                        building.BuildingId,
                        billingPeriod);
                }
                else
                {
                    _logger.LogWarning(
                        "MonthlyBillingJob failed to generate invoices for building {BuildingId} ({BillingPeriod}). Reason: {Message}",
                        building.BuildingId,
                        billingPeriod,
                        result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "MonthlyBillingJob encountered an error while generating invoices for building {BuildingId} ({BillingPeriod}).",
                    building.BuildingId,
                    billingPeriod);
            }
        }
    }

    //vòng lặp để tính thời gian chờ đến lần chạy kế tiếp vào 07:00 hôm sau.
    private static TimeSpan GetDelayUntilNextRun()
    {
		var now = DateTime.Now;
		var nextRun = new DateTime(now.Year, now.Month, now.Day, 22, 15 , 0); // Mốc 07:00 hôm nay

        if (now >= nextRun)
        {
			nextRun = nextRun.AddDays(1); 
        }

		return nextRun - now; 
    }
}


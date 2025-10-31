using ApartaAPI.DTOs.MeterReadings;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;

namespace ApartaAPI.Services;

public class MeterReadingService : IMeterReadingService
{
    private readonly IMeterReadingRepository _meterReadingRepo;
    private readonly IRepository<Apartment> _apartmentRepo;
    private readonly IRepository<Meter> _meterRepo;
    private readonly IPriceQuotationRepository _priceQuotationRepo;
    private readonly IRepository<Invoice> _invoiceRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Building> _buildingRepo;
    private readonly IMapper _mapper;

    public MeterReadingService(
        IMeterReadingRepository meterReadingRepo,
        IRepository<Apartment> apartmentRepo,
        IRepository<Meter> meterRepo,
        IPriceQuotationRepository priceQuotationRepo,
        IRepository<Invoice> invoiceRepo,
        IRepository<User> userRepo,
        IRepository<Building> buildingRepo,
        IMapper mapper)
    {
        _meterReadingRepo = meterReadingRepo;
        _apartmentRepo = apartmentRepo;
        _meterRepo = meterRepo;
        _priceQuotationRepo = priceQuotationRepo;
        _invoiceRepo = invoiceRepo;
        _userRepo = userRepo;
        _buildingRepo = buildingRepo;
        _mapper = mapper;
    }
    //danh sách căn hộ và thông tin đồng hồ để ghi chỉ số
    public async Task<List<ApartmentMeterInfoDto>> GetApartmentsForRecordingAsync(string buildingCode, string billingPeriod)
    {
        // Lấy các căn hộ Đã thuê trong building có buildingCode
        var apartments = await _apartmentRepo.FindAsync(
            a => a.Status == "Đã thuê" && a.Building.BuildingCode == buildingCode
        );
        var meters = await _meterRepo.FindAsync(m => m.Status == "ACTIVE");

        var result = new List<ApartmentMeterInfoDto>();

        foreach (var apt in apartments)
        {
            var aptInfo = new ApartmentMeterInfoDto
            {
                ApartmentId = apt.ApartmentId,
                ApartmentCode = apt.Code,
                BuildingId = apt.BuildingId,
                Meters = new List<MeterInfoDto>()
            };

            foreach (var meter in meters)
            {
                var currentReading = await _meterReadingRepo.GetByApartmentAndPeriodAsync(
                    apt.ApartmentId,
                    meter.MeterId,
                    billingPeriod
                );

                var lastReading = await _meterReadingRepo.GetLatestReadingAsync(
                    apt.ApartmentId,
                    meter.MeterId,
                    billingPeriod
                );

                aptInfo.Meters.Add(new MeterInfoDto
                {
                    MeterId = meter.MeterId,
                    MeterType = meter.Type,
                    LastReading = lastReading?.CurrentReading,
                    CurrentReading = currentReading?.CurrentReading,
                    IsRecorded = currentReading != null,
                    ReadingDate = currentReading?.ReadingDate,
                    RecordedByName = currentReading?.RecordedByUser?.Name
                });
            }

            result.Add(aptInfo);
        }

        return result;
    }

    public async Task<MeterReadingDto> RecordMeterReadingAsync(
        RecordMeterReadingRequest request,
        string staffId,
        string billingPeriod)
    {
        // Validate input
        if (request.CurrentReading < 0)
        {
            throw new ArgumentException("Current reading cannot be negative");
        }

        // Resolve staff user: accept either user_id or staff_code
        var staffUser = await _userRepo.FirstOrDefaultAsync(u => u.UserId == staffId || u.StaffCode == staffId);
        if (staffUser == null)
        {
            throw new ArgumentException("Staff not found. Please pass a valid user_id or staff_code");
        }
        var recordedById = staffUser.UserId;

        // Get apartment and meter for validation
        var apartment = await _apartmentRepo.GetByIdAsync(request.ApartmentId);
        if (apartment == null)
        {
            throw new ArgumentException("Apartment not found");
        }

        var meter = await _meterRepo.GetByIdAsync(request.MeterId);
        if (meter == null)
        {
            throw new ArgumentException("Meter not found");
        }

        // Check if reading already exists (UPSERT logic)
        var existingReading = await _meterReadingRepo.GetByApartmentAndPeriodAsync(
            request.ApartmentId,
            request.MeterId,
            billingPeriod
        );

        MeterReading reading;
        int previousReading;

        if (existingReading != null)
        {
            // UPDATE case
            existingReading.CurrentReading = request.CurrentReading;
            existingReading.ReadingDate = DateOnly.FromDateTime(DateTime.Now);
            existingReading.RecordedBy = recordedById;
            existingReading.UpdatedAt = DateTime.Now;

            await _meterReadingRepo.UpdateAsync(existingReading);
            reading = existingReading;
            previousReading = reading.PreviousReading;
        }
        else
        {
            // INSERT case - get previous reading from last month
            var lastReading = await _meterReadingRepo.GetLatestReadingAsync(
                request.ApartmentId,
                request.MeterId,
                billingPeriod
            );

            previousReading = lastReading?.CurrentReading ?? 0;

            reading = new MeterReading
            {
                MeterReadingId = Guid.NewGuid().ToString(),
                ApartmentId = request.ApartmentId,
                MeterId = request.MeterId,
                PreviousReading = previousReading,
                CurrentReading = request.CurrentReading,
                ReadingDate = DateOnly.FromDateTime(DateTime.Now),
                BillingPeriod = billingPeriod,
                RecordedBy = recordedById,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _meterReadingRepo.AddAsync(reading);

            // Reload to get navigation properties
            reading = await _meterReadingRepo.GetByApartmentAndPeriodAsync(
                request.ApartmentId,
                request.MeterId,
                billingPeriod
            ) ?? reading;
        }

        // Calculate consumption
        var consumption = reading.CurrentReading - previousReading;

        // Validate consumption
        if (consumption < 0)
        {
            throw new InvalidOperationException(
                $"Invalid consumption: current reading ({reading.CurrentReading}) is less than previous reading ({previousReading})"
            );
        }

        // Calculate estimated cost
        var estimatedCost = await CalculateCostAsync(request.MeterId, apartment.BuildingId, consumption);

        // Map using AutoMapper
        var dto = _mapper.Map<MeterReadingDto>(reading);
        dto.PreviousReading = previousReading;
        dto.Consumption = consumption;
        dto.EstimatedCost = estimatedCost;
        
        return dto;
    }

    public async Task<decimal> CalculateCostAsync(string meterId, string buildingId, int consumption)
    {
        if (consumption <= 0) return 0;

        // Get meter to determine type
        var meter = await _meterRepo.GetByIdAsync(meterId);
        if (meter == null) return 0;

        // ID cố định lấy từ cấu hình
        const string ELECTRIC_QUOTATION_ID = "6ad6c5c2-11fc-4a7b-bca7-b9a60535900d";
        const string WATER_QUOTATION_ID    = "d61950be-8cb5-4c51-9036-d6676d37292a";

        string quotationId;
        if (meter.Type.Equals("ELECTRIC", StringComparison.OrdinalIgnoreCase))
        {
            quotationId = ELECTRIC_QUOTATION_ID;
        }
        else if (meter.Type.Equals("WATER", StringComparison.OrdinalIgnoreCase))
        {
            quotationId = WATER_QUOTATION_ID;
        }
        else
        {
            return 0;
        }

        // Get price quotation directly by ID
        var priceQuotation = await _priceQuotationRepo.GetByIdAsync(quotationId);
        
        if (priceQuotation == null)
        {
            return 0;
        }

        // Tính đơn giản: consumption * unitPrice
        return consumption * priceQuotation.UnitPrice;
    }

    public async Task<int> GenerateMonthlyInvoicesAsync(string buildingId, string billingPeriod)
    {
        // Get only readings that haven't been invoiced yet
        var readings = await _meterReadingRepo.GetByBuildingAndPeriodAsync(buildingId, billingPeriod);
        var uninvoicedReadings = readings.Where(r => !r.IsInvoiced).ToList();

        if (!uninvoicedReadings.Any())
        {
            return 0;
        }

        // Group readings by apartment
        var apartmentGroups = uninvoicedReadings.GroupBy(r => r.ApartmentId);

        int invoiceCount = 0;
        var readingsToMarkAsInvoiced = new List<MeterReading>();

        foreach (var group in apartmentGroups)
        {
            var apartmentId = group.Key;
            decimal totalCost = 0;
            var descriptionParts = new List<string>();

            foreach (var reading in group)
            {
                var consumption = reading.CurrentReading - reading.PreviousReading;
                var cost = await CalculateCostAsync(reading.MeterId, buildingId, consumption);

                totalCost += cost;

                var meterTypeName = reading.Meter.Type == "ELECTRIC" ? "Điện" : "Nước";
                var unit = reading.Meter.Type == "ELECTRIC" ? "kWh" : "m³";

                descriptionParts.Add($"{meterTypeName}: {consumption} {unit} = {cost:N0} VND");
            }

            // Create invoice even if totalCost is 0 to mark readings as invoiced
            // This prevents duplicate invoice generation

            // Parse billing period to get start and end dates
            // Invoice dates should be for the NEXT month (not current month)
            // Example: If billingPeriod is 2025-10 (October), invoice dates should be for November (2025-11)
            var periodParts = billingPeriod.Split('-');
            if (periodParts.Length != 2) continue;

            var year = int.Parse(periodParts[0]);
            var month = int.Parse(periodParts[1]);
            
            // Calculate next month for invoice dates
            var nextMonthDate = new DateOnly(year, month, 1).AddMonths(1);
            var startDate = new DateOnly(nextMonthDate.Year, nextMonthDate.Month, 1); // First day of next month
            var endDate = startDate.AddMonths(1).AddDays(-1); // Last day of next month

            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid().ToString(),
                ApartmentId = apartmentId,
                FeeType = "UTILITY",
                Price = totalCost,
                Status = "PENDING",
                Description = string.Join("; ", descriptionParts),
                StartDate = startDate,
                EndDate = endDate,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _invoiceRepo.AddAsync(invoice);
            invoiceCount++;

            // Mark all readings in this group as invoiced
            readingsToMarkAsInvoiced.AddRange(group);
        }

        // Mark all used readings as invoiced
        foreach (var reading in readingsToMarkAsInvoiced)
        {
            reading.IsInvoiced = true;
            reading.UpdatedAt = DateTime.Now;
            await _meterReadingRepo.UpdateAsync(reading);
        }

        return invoiceCount;
    }

    public async Task<RecordingProgressDto> GetRecordingProgressAsync(string buildingCode, string billingPeriod)
    {
        // First get the building by code
        var building = await _buildingRepo.FirstOrDefaultAsync(b => b.BuildingCode == buildingCode);
        if (building == null)
        {
            throw new KeyNotFoundException($"Building with code '{buildingCode}' not found");
        }

        // Only count apartments that are rented (Đã thuê) - same as recording sheet
        var apartments = await _apartmentRepo.FindAsync(a => a.BuildingId == building.BuildingId && a.Status == "Đã thuê");
        var totalApartments = apartments.Count();

        var meters = await _meterRepo.FindAsync(m => m.Status == "ACTIVE");

        var recordedByMeterType = new Dictionary<string, int>();
        var progressByMeterType = new Dictionary<string, decimal>();

        foreach (var meter in meters)
        {
            var recordedCount = await _meterReadingRepo.CountRecordedReadingsAsync(
                building.BuildingId,
                billingPeriod,
                meter.MeterId
            );

            recordedByMeterType[meter.Type] = recordedCount;
            progressByMeterType[meter.Type] = totalApartments > 0
                ? Math.Round((decimal)recordedCount / totalApartments * 100, 2)
                : 0;
        }

        return new RecordingProgressDto
        {
            BuildingId = building.BuildingId,
            BillingPeriod = billingPeriod,
            TotalApartments = totalApartments,
            RecordedByMeterType = recordedByMeterType,
            ProgressByMeterType = progressByMeterType,
            LastUpdated = DateTime.Now
        };
    }

    public async Task<List<MeterReadingDto>> GetReadingHistoryAsync(string apartmentId, string meterId, int limit = 12)
    {
        var readings = await _meterReadingRepo.GetReadingHistoryAsync(apartmentId, meterId, limit);
        var apartment = await _apartmentRepo.GetByIdAsync(apartmentId);
        var meter = await _meterRepo.GetByIdAsync(meterId);

        var result = new List<MeterReadingDto>();

        foreach (var reading in readings)
        {
            var consumption = reading.CurrentReading - reading.PreviousReading;
            var cost = apartment != null ? await CalculateCostAsync(meterId, apartment.BuildingId, consumption) : 0;

            var dto = _mapper.Map<MeterReadingDto>(reading);
            dto.Consumption = consumption;
            dto.EstimatedCost = cost;
            result.Add(dto);
        }

        return result;
    }

    public async Task<List<MeterReadingDto>> GetRecordedReadingsByPeriodAsync(string buildingCode, string billingPeriod)
    {
        // First get the building by code
        var building = await _buildingRepo.FirstOrDefaultAsync(b => b.BuildingCode == buildingCode);
        if (building == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy tòa nhà với mã: {buildingCode}");
        }

        // Lấy tất cả readings đã ghi trong billing period
        var readings = await _meterReadingRepo.GetByBuildingAndPeriodAsync(building.BuildingId, billingPeriod);

        var result = new List<MeterReadingDto>();

        foreach (var reading in readings)
        {
            try
            {
                var consumption = reading.CurrentReading - reading.PreviousReading;
                var cost = await CalculateCostAsync(reading.MeterId, building.BuildingId, consumption);

                var dto = _mapper.Map<MeterReadingDto>(reading);
                dto.Consumption = consumption;
                dto.EstimatedCost = cost;
                if (string.IsNullOrEmpty(dto.BillingPeriod))
                {
                    dto.BillingPeriod = billingPeriod;
                }
                result.Add(dto);
            }
            catch (Exception ex)
            {
                // Log the error but continue with other readings
                Console.WriteLine($"Error processing reading {reading.MeterReadingId}: {ex.Message}");
            }
        }

        return result;
    }
}

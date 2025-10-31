namespace ApartaAPI.DTOs.MeterReadings;

public class RecordingProgressDto
{
    public string BuildingId { get; set; } = null!;
    public string BillingPeriod { get; set; } = null!;
    public int TotalApartments { get; set; }
    public Dictionary<string, int> RecordedByMeterType { get; set; } = new();
    public Dictionary<string, decimal> ProgressByMeterType { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}


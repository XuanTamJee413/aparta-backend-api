namespace ApartaAPI.DTOs.MeterReadings;

public class MeterReadingDto
{
    public string MeterReadingId { get; set; } = null!;
    public string ApartmentId { get; set; } = null!;
    public string ApartmentCode { get; set; } = null!;
    public string MeterId { get; set; } = null!;
    public string MeterType { get; set; } = null!;
    public int PreviousReading { get; set; }
    public int CurrentReading { get; set; }
    public int Consumption { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateOnly ReadingDate { get; set; }
    public string BillingPeriod { get; set; } = null!;
    public string? RecordedByName { get; set; }
    public DateTime? RecordedAt { get; set; }
}


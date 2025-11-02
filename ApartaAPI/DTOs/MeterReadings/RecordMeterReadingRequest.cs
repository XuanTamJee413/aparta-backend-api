namespace ApartaAPI.DTOs.MeterReadings;

public class RecordMeterReadingRequest
{
    public string ApartmentId { get; set; } = null!;
    public string MeterId { get; set; } = null!;
    public int CurrentReading { get; set; }
}


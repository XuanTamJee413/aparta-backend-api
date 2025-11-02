namespace ApartaAPI.DTOs.MeterReadings;

public class MeterInfoDto
{
    public string MeterId { get; set; } = null!;
    public string MeterType { get; set; } = null!;
    public int? LastReading { get; set; }
    public int? CurrentReading { get; set; }
    public bool IsRecorded { get; set; }
    public DateOnly? ReadingDate { get; set; }
    public string? RecordedByName { get; set; }
}


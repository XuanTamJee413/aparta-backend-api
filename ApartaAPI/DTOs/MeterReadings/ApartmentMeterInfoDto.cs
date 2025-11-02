namespace ApartaAPI.DTOs.MeterReadings;

public class ApartmentMeterInfoDto
{
    public string ApartmentId { get; set; } = null!;
    public string ApartmentCode { get; set; } = null!;
    public string BuildingId { get; set; } = null!;
    public List<MeterInfoDto> Meters { get; set; } = new();
}


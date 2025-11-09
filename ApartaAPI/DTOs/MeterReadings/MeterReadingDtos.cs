using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.MeterReadings
{
    public sealed record MeterReadingCreateDto(
        [Required]
        string FeeType,
        [Required]
        decimal ReadingValue,
        [Required]
        DateOnly ReadingDate
    );

    public sealed record MeterReadingUpdateDto(
        [Required]
        decimal ReadingValue
    );

    public sealed record MeterReadingDto(
        string MeterReadingId,
        string ApartmentId,
        string FeeType,
        decimal ReadingValue,
        DateOnly ReadingDate,
        DateTime? CreatedAt,
        DateTime? UpdatedAt,
        string? RecordedBy,
        string? BillingPeriod,
        string? InvoiceItemId
    );

    public sealed record MeterReadingCheckResponse(
        bool Exists,
        MeterReadingDto? MeterReading,
        MeterReadingDto? LatestReading // Chỉ số mới nhất trước đó (nếu có)
    );

    public sealed record MeterReadingStatusDto(
        string ApartmentId,
        string ApartmentCode,
        string FeeType,
        decimal? ReadingValue,
        string? ReadingId,
        DateOnly? ReadingDate,
        string? RecordedByName,
        string? InvoiceItemId,
        string Status // "Đã ghi - Đã khóa", "Đã ghi", hoặc "Chưa ghi"
    );
}


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
}


using System;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Subscriptions
{
    public sealed record SubscriptionQueryParameters(
        DateTime? FromDate,
        DateTime? ToDate,
        string? DateType,
        string? Status,
        int Skip = 0,
        int Take = 10
    );

    public sealed record SubscriptionDto
    {
        public string SubscriptionId { get; init; } = default!;
        public string ProjectId { get; init; } = default!;
        public string ProjectName { get; init; } = default!;
        public string ProjectCode { get; init; } = default!;
        public string SubscriptionCode { get; init; } = default!;
        public string Status { get; init; } = default!;
        public decimal Amount { get; init; }
        public int NumMonths { get; init; }
        public DateTime ExpiredAt { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public decimal? AmountPaid { get; init; }
        public DateTime? PaymentDate { get; init; }
        public string? PaymentMethod { get; init; }
        public string? PaymentNote { get; init; }
    }

    public sealed record SubscriptionCreateOrUpdateDto(
        [Required]
        string ProjectId,

        [Required]
        string SubscriptionCode,

        [Required]
        [Range(1, int.MaxValue)]
        int NumMonths,

        [Required]
        [Range(0, double.MaxValue)]
        decimal Amount,

        [Range(0, double.MaxValue)]
        decimal? AmountPaid,

        string? PaymentMethod,
        DateTime? PaymentDate,
        string? PaymentNote,

        [Required]
        bool IsApproved
    );
}
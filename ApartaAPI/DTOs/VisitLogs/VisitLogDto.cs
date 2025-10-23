using System;

namespace ApartaAPI.DTOs.VisitLogs
{
    public sealed record VisitLogDto(
        string Id,
        string? ApartmentId,
        string? VisitorId,
        DateTime? CheckinTime,
        DateTime? CheckoutTime,
        string? Purpose,
        string? Status
    );
    public class VisitLogHistoryDto
    {
        public string VisitLogId { get; set; } = null!;
        public string? VisitorName { get; set; }
        public string? Purpose { get; set; }
        public DateTime? CheckinTime { get; set; }
        public DateTime? CheckoutTime { get; set; }
        public string? Status { get; set; }
    }

    public sealed record VisitLogCreateDto(
        string? ApartmentId,
        string? VisitorId,
        string? Purpose
    );

    public sealed record VisitLogUpdateDto(
        DateTime? CheckoutTime,
        string? Status
    );
}
using System;

namespace ApartaAPI.DTOs.VisitLogs
{
    // cai nay hien thi tren staff view visitor list
    public class VisitLogStaffViewDto
    {
        // VisitLog
        public string VisitLogId { get; set; } = null!;
        public DateTime CheckinTime { get; set; }
        public DateTime? CheckoutTime { get; set; }
        public string? Purpose { get; set; }
        public string Status { get; set; } = null!;

        // Apartment
        public string ApartmentCode { get; set; } = null!;

        // Visitor
        public string VisitorFullName { get; set; } = null!;
        public string? VisitorIdNumber { get; set; }
    }
    public class VisitLogDto
    {
        public string VisitLogId { get; init; } = null!; 
        public string? ApartmentId { get; init; }
        public string? VisitorId { get; init; }
        public DateTime? CheckinTime { get; init; }
        public DateTime? CheckoutTime { get; init; }
        public string? Purpose { get; init; }
        public string? Status { get; init; }
    }
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
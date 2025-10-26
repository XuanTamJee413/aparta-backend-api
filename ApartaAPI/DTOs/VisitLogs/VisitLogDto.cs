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
}
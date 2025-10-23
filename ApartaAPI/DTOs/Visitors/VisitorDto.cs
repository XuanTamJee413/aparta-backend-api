namespace ApartaAPI.DTOs.Visitors
{
    public sealed record VisitorDto(
      string VisitorId,
      string? FullName,
      string? Phone,
      string? IdNumber
    );

    public class VisitorCreateDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? IdNumber { get; set; }

        public string? ApartmentId { get; set; }
        public string? Purpose { get; set; }
        public DateTime? CheckinTime { get; set; }
        public string? Status { get; set; }
    }

    public sealed record VisitorUpdateDto(
      string? FullName,
      string? Phone,
      string? IdNumber
    );
}
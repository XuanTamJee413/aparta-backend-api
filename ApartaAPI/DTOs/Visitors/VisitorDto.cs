namespace ApartaAPI.DTOs.Visitors
{
    public sealed record VisitorDto(
      string VisitorId,
      string? FullName,
      string? Phone,
      string? IdNumber
    );

    public sealed record VisitorCreateDto(
      string? VisitorId,
      string? FullName,
      string? Phone,
      string? IdNumber,

      string? ApartmentId,
      string? Purpose
    );

    public sealed record VisitorUpdateDto(
      string? FullName,
      string? Phone,
      string? IdNumber
    );
}
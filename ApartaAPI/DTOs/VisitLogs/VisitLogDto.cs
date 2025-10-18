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
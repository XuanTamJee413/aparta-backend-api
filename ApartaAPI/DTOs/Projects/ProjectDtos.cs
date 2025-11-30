using System;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Projects
{
    public sealed record ProjectQueryParameters(
        bool? IsActive,
        string? SearchTerm,
        string? SortBy,
        string? SortOrder
    );

    // [READ] Output: Thêm Address + Bank + Counts (tính toán)
    public sealed record ProjectDto(
        string ProjectId,
        string? ProjectCode,
        string? Name,

        // Nhóm Địa chỉ
        string? Address,
        string? Ward,
        string? District,
        string? City,

        // Nhóm Ngân hàng
        string? BankName,
        string? BankAccountNumber,
        string? BankAccountName,

        // Nhóm PayOS
        string? PayOSClientId,
        string? PayOSApiKey,
        string? PayOSChecksumKey,

        // Thống kê (Calculated)
        int NumApartments,
        int NumBuildings,

        DateTime? CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    );

    // [CREATE] Input: Thêm Address + Bank. BỎ NumApartments/NumBuildings
    public sealed record ProjectCreateDto(
        string? ProjectCode,
        string? Name,
        string? Address,
        string? Ward,
        string? District,
        string? City,
        string? BankName,
        string? BankAccountNumber,
        string? BankAccountName,
        string? PayOSClientId,
        string? PayOSApiKey,
        string? PayOSChecksumKey
    );

    // [UPDATE] Input: Thêm Address + Bank. BỎ NumApartments/NumBuildings
    public sealed record ProjectUpdateDto(
        string? Name,
        string? Address,
        string? Ward,
        string? District,
        string? City,
        string? BankName,
        string? BankAccountNumber,
        string? BankAccountName,
        string? PayOSClientId,
        string? PayOSApiKey,
        string? PayOSChecksumKey,
        bool? IsActive
    );
}
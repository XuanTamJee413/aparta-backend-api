using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Contracts
{
    public sealed record ContractQueryParameters(
        string? ApartmentId,
        string? SortBy,
        string? SortOrder
    );

    public sealed record ContractDto(
        string ContractId,
        string ApartmentId,
        string? Image,
        DateOnly? StartDate,
        DateOnly? EndDate,
        DateTime? CreatedAt
    );

    
    public sealed record ContractCreateDto(
        [Required]
        string ApartmentId,
        string? Image,
        DateOnly? StartDate,
        DateOnly? EndDate,
        [Required(ErrorMessage = "Tên chủ hộ là bắt buộc")]
        string OwnerName,
        string? OwnerPhoneNumber,
        string? OwnerIdNumber,
        string? OwnerEmail,
        string? OwnerGender,
        DateOnly? OwnerDateOfBirth,
        string? OwnerNationality
    );

    public sealed record ContractUpdateDto(
        string? Image,
        DateOnly? StartDate,
        DateOnly? EndDate
    );
}
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Contracts
{
    public sealed record ContractQueryParameters(
        string? ApartmentId,
        string? SortBy,
        string? SortOrder
    );

    public sealed record ContractDto
    {
        public string ContractId { get; init; } = default!;
        public string ApartmentId { get; init; } = default!;
        public string? ApartmentCode { get; init; }

        public string? OwnerName { get; init; }
        public string? OwnerPhoneNumber { get; init; }
        public string? OwnerEmail { get; init; }

        public string? Image { get; init; }

        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }

        public DateTime? CreatedAt { get; init; }

        public string ContractType { get; set; } = "Sale";
        public decimal? DepositAmount { get; set; }
        public decimal? TotalValue { get; set; }
    }


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

    public sealed record ContractUpdateDto
    {
        public DateOnly? EndDate { get; set; }
        public IFormFile? ImageFile { get; set; }  
        public string? Image { get; set; }         
    }
}
namespace ApartaAPI.DTOs
{
    public class CreateContractRequestDto
    {
        public required string ApartmentId { get; set; }
        public required string ContractNumber { get; set; }
        public required string ContractType { get; set; } // "Lease" hoặc "Sale"

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public required List<MemberInputDto> Members { get; set; }
    }

    public class MemberInputDto
    {
        public required string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IdentityCard { get; set; }

        // Trường này sẽ map vào bảng [User], KHÔNG map vào [ApartmentMember]
        public string? Email { get; set; }

        // "owner", "tenant", "family_member"
        public required string RoleName { get; set; }

        public bool IsAppAccess { get; set; }
        public bool IsRepresentative { get; set; }
    }
}
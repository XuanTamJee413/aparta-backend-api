using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.PayOS;

public sealed record PayOSValidationRequest(
    [Required] string ClientId,
    [Required] string ApiKey,
    [Required] string ChecksumKey
);

public sealed record PayOSValidationResponse(
    bool IsValid,
    string? Message,
    string? BankName = null,
    string? AccountNumber = null,
    string? AccountName = null
);


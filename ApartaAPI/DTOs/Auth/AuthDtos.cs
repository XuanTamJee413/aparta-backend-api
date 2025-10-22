namespace ApartaAPI.DTOs.Auth
{
    // Login request
    public sealed record LoginRequest(
        string Phone,
        string Password
    );

    // Register request
    public sealed record RegisterRequest(
        string Phone,
        string Password,
        string ConfirmPassword,
        string Name,
        string Email,
        string RoleId
    );

    // Login response
    public sealed record LoginResponse(
        string Token
    );

    // Register response
    public sealed record RegisterResponse(
        string UserId,
        string Name,
        string Phone,
        string Email,
        string Role,
        string Message
    );

    // User info response
    public sealed record UserInfoResponse(
        string UserId,
        string Name,
        string Phone,
        string Email,
        string Role,
        string? ApartmentId,
        string? StaffCode,
        string Status,
        DateTime? LastLoginAt
    );

    public sealed record ProfileUpdateDto(
        string? Name,
        string? Email,
        string? Phone,
        string? AvatarUrl
    );
}

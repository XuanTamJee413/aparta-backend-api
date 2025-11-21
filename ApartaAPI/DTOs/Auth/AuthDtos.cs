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

    // Forgot password request
    public sealed record ForgotPasswordDto(
        string Email
    );

    // Reset password request
    public sealed record ResetPasswordDto(
        string Token,
        string Email,
        string NewPassword,
        string ConfirmPassword
    );

    public sealed record ProfileUpdateDto(
        string? Name,
        string? Email,
        string? Phone,
        string? AvatarUrl
    );
}

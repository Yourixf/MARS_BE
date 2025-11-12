namespace MARS_BE.Features.Auth;

public sealed class RegisterDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    // public Guid TenantId { get; init; } = Guid.Empty;
    // public string[]? Permissions { get; init; } // e.g. ["employees.read","employees.write"]
}

public sealed class LoginDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class TokenResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; init; }
    // public string RefreshToken { get; init; } = string.Empty;
    // public DateTime RefreshTokenExpiresAt { get; init; }
}

// public sealed class RefreshRequestDto
// {
//     public string RefreshToken { get; init; } = string.Empty;
//     public string? DeviceId { get; init; }
// }
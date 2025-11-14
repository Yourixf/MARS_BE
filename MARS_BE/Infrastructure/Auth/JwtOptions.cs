namespace MARS_BE.Infrastructure.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string Key { get; set; } = default!; // ≥32 chars voor HS256
}
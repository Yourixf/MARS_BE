using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MARS_BE.Features.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MARS_BE.Infrastructure.Auth;

public interface IJwtTokenService
{
    string CreateAccessToken(ApplicationUser user, IEnumerable<Claim> extraClaims);
    (string token, DateTime expiresAt) CreateRefreshToken();
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opt;
    private readonly SymmetricSecurityKey _key;

    public JwtTokenService(IOptions<JwtOptions> opt)
    {
        _opt = opt.Value;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
    }

    public string CreateAccessToken(ApplicationUser user, IEnumerable<Claim> extraClaims)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("name", user.DisplayName ?? user.UserName ?? string.Empty),
        };
        claims.AddRange(extraClaims);

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public (string token, DateTime expiresAt) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var expires = DateTime.UtcNow.AddDays(_opt.RefreshTokenDays);
        return (token, expires);
    }
}
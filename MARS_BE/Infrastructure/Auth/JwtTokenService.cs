using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MARS_BE.Infrastructure.Auth;

// Defines a contract for token generation (Interface)
public interface IJwtTokenService
{
    string GenerateToken(string username);
}

// Injects JwtOptions 
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(Microsoft.Extensions.Options.IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    // Makes an array of claims
    public string GenerateToken(string username)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Turns secret key into bytes, makes SigningCredentials object with HS256 algorithm
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Builds JWT token
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        // Serializes token to a string (what is send to client)
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
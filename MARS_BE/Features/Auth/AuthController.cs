using System.Security.Claims;
using MARS_BE.Features.Users;
using MARS_BE.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MARS_BE.Data;

namespace MARS_BE.Features.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole<Guid>> _roles;
    private readonly IJwtTokenService _tokens;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole<Guid>> roles,
        IJwtTokenService tokens,
        AppDbContext db)
    {
        _users = users;
        _roles = roles;
        _tokens = tokens;
        _db = db;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterDto dto)
    {
        var existing = await _users.FindByEmailAsync(dto.Email);
        if (existing is not null) return Conflict("Email already in use.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            TenantId = dto.TenantId == Guid.Empty ? Guid.NewGuid() : dto.TenantId,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        // Add permission claims
        if (dto.Permissions is not null)
        {
            var claims = dto.Permissions.Select(p => new Claim("perm", p));
            await _users.AddClaimsAsync(user, claims);
        }

        return Created($"/api/v1/users/{user.Id}", new { user.Id, user.TenantId, user.Email });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !user.IsActive) return Unauthorized();

        if (!await _users.CheckPasswordAsync(user, dto.Password)) return Unauthorized();

        var userClaims = await _users.GetClaimsAsync(user);
        var access = _tokens.CreateAccessToken(user, userClaims);

        var (rt, rtExp) = _tokens.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = rt,
            ExpiresAt = rtExp,
            CreatedAt = DateTime.UtcNow,
            DeviceId = dto.DeviceId,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });
        await _db.SaveChangesAsync();

        return Ok(new TokenResponseDto
        {
            AccessToken = access,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(30),
            RefreshToken = rt,
            RefreshTokenExpiresAt = rtExp
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == dto.RefreshToken);
        if (rt is null || !rt.IsActive) return Unauthorized();

        var user = await _users.FindByIdAsync(rt.UserId.ToString());
        if (user is null || !user.IsActive) return Unauthorized();

        // rotate refresh token
        rt.RevokedAt = DateTime.UtcNow;

        var (newRt, newRtExp) = _tokens.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRt,
            ExpiresAt = newRtExp,
            CreatedAt = DateTime.UtcNow,
            DeviceId = dto.DeviceId,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });

        var userClaims = await _users.GetClaimsAsync(user);
        var access = _tokens.CreateAccessToken(user, userClaims);
        await _db.SaveChangesAsync();

        return Ok(new TokenResponseDto
        {
            AccessToken = access,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(30),
            RefreshToken = newRt,
            RefreshTokenExpiresAt = newRtExp
        });
    }

    [HttpPost("logout")]
    [Authorize] // revoke all user's refresh tokens on this device
    public async Task<ActionResult> Logout([FromBody] RefreshRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var tokens = await _db.RefreshTokens
            .Where(x => x.UserId == userId && (dto.DeviceId == null || x.DeviceId == dto.DeviceId) && x.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

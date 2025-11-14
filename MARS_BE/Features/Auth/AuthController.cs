using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MARS_BE.Features.Users;
using MARS_BE.Infrastructure.Auth;
using MARS_BE.Features.Auth;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwt;
    private readonly IMapper _mapper;


    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwt,
        IMapper mapper)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
        _mapper = mapper;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        // Map all basic fields from DTO to ApplicationUser using AutoMapper
        var user = _mapper.Map<ApplicationUser>(dto);

        // Still generate Id in code (Identity will handle the password hashing)
        user.Id = Guid.NewGuid();

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(e => e.Description)
            });
        }

        // Temporary: assign a basic permission so the user can access Employees GET endpoints
        await _userManager.AddClaimAsync(user, new Claim("perm", "employees.read"));

        return CreatedAtAction(nameof(Me), null);
    }


    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null || !user.IsActive)
            return Unauthorized();

        // Validate password (does not lock out the user on failure)
        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(
            user,
            dto.Password,
            lockoutOnFailure: false
        );

        if (!passwordCheck.Succeeded)
            return Unauthorized();

        // Fetch user roles and claims from the database
        var roles = await _userManager.GetRolesAsync(user);
        var claims = (await _userManager.GetClaimsAsync(user)).ToList();

        // Standard JWT identity claims
        claims.Add(new Claim("sub", user.Id.ToString()));
        claims.Add(new Claim("email", user.Email ?? string.Empty));

        // Optional: include role claims in the access token
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // Generate access token with a 15-minute lifetime
        var accessToken = _jwt.CreateAccessToken(claims, TimeSpan.FromMinutes(15));

        return Ok(new TokenResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            sub = User.FindFirstValue("sub"),
            email = User.FindFirstValue("email"),
            perms = User.Claims
                .Where(c => c.Type == "perm")
                .Select(c => c.Value)
                .ToArray()
        });
    }
}

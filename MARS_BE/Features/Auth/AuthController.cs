using Microsoft.AspNetCore.Mvc;
using MARS_BE.Infrastructure.Auth;

namespace MARS_BE.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtService;

    public AuthController(IJwtTokenService jwtService)
    {
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username == "admin" && request.Password == "password")
        {
            var token = _jwtService.GenerateToken(request.Username);
            return Ok(new { token });
        }

        return Unauthorized();
    }

    [HttpGet("secure")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult SecureEndpoint()
    {
        return Ok("You are authorized!");
    }
}

public record LoginRequest(string Username, string Password);
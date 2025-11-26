using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MARS_BE.Common.Utils;

namespace MARS_BE.Features.Users;

[ApiController]
[Route("api/v1/users")]
[Authorize] // keep simple for now; later add policies like [Authorize(Policy = Permissions.UsersRead)]
public sealed class UsersController : ControllerBase
{
    private readonly IUsersService _service;

    public UsersController(IUsersService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserReadDto>>> GetAll([FromQuery] UsersQuery q)
        => Ok(await _service.GetAllAsync(q));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserReadDto>> GetById(Guid id, [FromQuery] bool includeInactive = false)
    {
        var dto = await _service.GetByIdAsync(id, includeInactive);
        return dto is null ? NotFound() : Ok(dto);
    }

    // Admin-driven create (not self-register)
    [HttpPost]
    public async Task<ActionResult<UserReadDto>> Create([FromBody] UserCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PATCH for partial updates
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<UserReadDto>> Update(Guid id, [FromBody] UserUpdateDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    // Soft delete / deactivate
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var ok = await _service.DeactivateAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
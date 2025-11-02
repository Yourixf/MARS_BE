using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MARS_BE.Common.Auth; 

namespace MARS_BE.Features.Employees;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class EmployeesController(IEmployeesService service) : ControllerBase
{
    // GET /api/v1/employees?search=&page=1&pageSize=20&includeInactive=false
    [Authorize(Policy = Permissions.EmployeesRead)]
    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] EmployeesQuery q)
    {
        if (q.Page <= 0 || q.PageSize is <= 0 or > 200)
            return BadRequest("Invalid pagination.");

        var result = await service.GetAllAsync(q);
        return Ok(result);
    }

    // GET /api/v1/employees/{id}
    [Authorize(Policy = Permissions.EmployeesRead)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id, [FromQuery] bool includeInactive = false)
    {
        var result = await service.GetByIdAsync(id, includeInactive);
        return result is null ? NotFound() : Ok(result);
    }

    // POST /api/v1/employees
    [Authorize(Policy = Permissions.EmployeesWrite)]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
    [Authorize(Policy = Permissions.EmployeesWrite)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Replace(Guid id, [FromBody] EmployeeReplaceDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var updated = await service.ReplaceAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }
    [Authorize(Policy = Permissions.EmployeesWrite)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult> Patch(Guid id, [FromBody] EmployeeUpdateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var updated = await service.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    // DELETE /api/v1/employees/{id}   (soft delete)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id)
        => await service.SoftDeleteAsync(id) ? NoContent() : NotFound();
}
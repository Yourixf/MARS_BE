using Microsoft.AspNetCore.Mvc;

namespace MARS_BE.Features.Employees;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class EmployeesController(IEmployeesService service) : ControllerBase
{
    // GET /api/v1/employees?search=&page=1&pageSize=20&includeInactive=false
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeInactive = false)
    {
        if (page <= 0 || pageSize is <= 0 or > 200)
            return BadRequest("Invalid pagination.");

        var result = await service.GetAllAsync(search, page, pageSize, includeInactive);
        return Ok(result);
    }

    // GET /api/v1/employees/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id, [FromQuery] bool includeInactive = false)
    {
        var result = await service.GetByIdAsync(id, includeInactive);
        return result is null ? NotFound() : Ok(result);
    }

    // POST /api/v1/employees
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Replace(Guid id, [FromBody] EmployeeReplaceDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var updated = await service.ReplaceAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }
    
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
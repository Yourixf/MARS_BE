using MARS_BE.Common;
using MARS_BE.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MARS_BE.Features.Employees;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmployeesController(AppDbContext db) => _db = db;

    // GET /api/v1/employees?search=&page=1&pageSize=20&includeInactive=false
    [HttpGet]
    public async Task<ActionResult<PagedResult<EmployeeReadDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeInactive = false)
    {
        if (page <= 0 || pageSize is <= 0 or > 200)
            return BadRequest("Invalid pagination.");

        IQueryable<Employee> q = _db.Employees.AsNoTracking();
        if (includeInactive) q = q.IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(e =>
                e.EmployeeNo.Contains(search) ||
                e.FirstName.Contains(search) ||
                e.LastName.Contains(search) ||
                e.Email.Contains(search));
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeReadDto
            {
                Id = e.Id,
                EmployeeNo = e.EmployeeNo,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                HireDate = e.HireDate,
                IsActive = e.IsActive
            })
            .ToListAsync();

        return Ok(new PagedResult<EmployeeReadDto>(items, page, pageSize, total));
    }

    // GET /api/v1/employees/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeReadDto>> GetById(Guid id, [FromQuery] bool includeInactive = false)
    {
        var q = _db.Employees.AsNoTracking();
        if (includeInactive) q = q.IgnoreQueryFilters();

        var e = await q.FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();

        return Ok(new EmployeeReadDto
        {
            Id = e.Id,
            EmployeeNo = e.EmployeeNo,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            HireDate = e.HireDate,
            IsActive = e.IsActive
        });
    }

    // POST /api/v1/employees
    [HttpPost]
    public async Task<ActionResult<EmployeeReadDto>> Create([FromBody] EmployeeCreateDto dto)
    {
        // ModelState bevat FluentValidation fouten automatisch
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // Uniekheid check (extra naast DB index voor nette foutmelding)
        var exists = await _db.Employees.IgnoreQueryFilters()
                           .AnyAsync(x => x.EmployeeNo == dto.EmployeeNo);
        if (exists) return Conflict($"EmployeeNo '{dto.EmployeeNo}' already exists.");

        var e = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNo = dto.EmployeeNo,
            FirstName = dto.FirstName,
            LastName  = dto.LastName,
            Email     = dto.Email,
            HireDate  = dto.HireDate,
            IsActive  = true
        };

        _db.Add(e);
        await _db.SaveChangesAsync();

        var read = new EmployeeReadDto
        {
            Id = e.Id,
            EmployeeNo = e.EmployeeNo,
            FirstName = e.FirstName,
            LastName  = e.LastName,
            Email     = e.Email,
            HireDate  = e.HireDate,
            IsActive  = e.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = e.Id }, read);
    }

    // PUT /api/v1/employees/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeReadDto>> Update(Guid id, [FromBody] EmployeeUpdateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var e = await _db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();

        if (dto.FirstName is not null) e.FirstName = dto.FirstName;
        if (dto.LastName  is not null) e.LastName  = dto.LastName;
        if (dto.Email     is not null) e.Email     = dto.Email;
        if (dto.HireDate  is not null) e.HireDate  = dto.HireDate.Value;
        if (dto.IsActive  is not null) e.IsActive  = dto.IsActive.Value;

        await _db.SaveChangesAsync();

        return Ok(new EmployeeReadDto
        {
            Id = e.Id,
            EmployeeNo = e.EmployeeNo,
            FirstName = e.FirstName,
            LastName  = e.LastName,
            Email     = e.Email,
            HireDate  = e.HireDate,
            IsActive  = e.IsActive
        });
    }

    // DELETE (soft) /api/v1/employees/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        var e = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();

        e.IsActive = false; // soft delete
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

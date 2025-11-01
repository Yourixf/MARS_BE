using MARS_BE.Common;
using MARS_BE.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS_BE.Features.Employees;

public sealed class EmployeesService(AppDbContext db) : IEmployeesService
{
    public async Task<PagedResult<EmployeeReadDto>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive)
    {
        IQueryable<Employee> q = db.Employees.AsNoTracking();
        if (includeInactive) q = q.IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(e =>
                e.EmployeeNo.Contains(search) ||
                e.FirstName.Contains(search)  ||
                e.LastName.Contains(search)   ||
                e.Email.Contains(search));
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeReadDto
            {
                Id         = e.Id,
                EmployeeNo = e.EmployeeNo,
                FirstName  = e.FirstName,
                LastName   = e.LastName,
                Email      = e.Email,
                HireDate   = e.HireDate,
                IsActive   = e.IsActive
            })
            .ToListAsync();

        return new PagedResult<EmployeeReadDto>(items, page, pageSize, total);
    }

    public async Task<EmployeeReadDto?> GetByIdAsync(Guid id, bool includeInactive)
    {
        var q = db.Employees.AsNoTracking();
        if (includeInactive) q = q.IgnoreQueryFilters();

        var e = await q.FirstOrDefaultAsync(x => x.Id == id);
        return e is null ? null : MapToRead(e);
    }

    public async Task<EmployeeReadDto> CreateAsync(EmployeeCreateDto dto)
    {
        // Uniekheidscheck e-mail (naast DB unique index voor nette fout)
        var existsEmail = await db.Employees.IgnoreQueryFilters()
                             .AnyAsync(x => x.Email == dto.Email);
        if (existsEmail) throw new ArgumentException($"Email '{dto.Email}' already exists.");

        var e = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNo = dto.EmployeeNo,         // immutable daarna
            FirstName  = dto.FirstName,
            LastName   = dto.LastName,
            Email      = dto.Email,
            HireDate   = dto.HireDate,
            IsActive   = true
        };

        db.Add(e);
        await db.SaveChangesAsync();
        return MapToRead(e);
    }

    // PATCH (partial)
    public async Task<EmployeeReadDto?> UpdateAsync(Guid id, EmployeeUpdateDto dto)
    {
        var e = await db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return null;

        if (dto.Email is not null && !string.Equals(dto.Email, e.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existsEmail = await db.Employees.IgnoreQueryFilters()
                                 .AnyAsync(x => x.Email == dto.Email);
            if (existsEmail) throw new ArgumentException($"Email '{dto.Email}' already exists.");
            e.Email = dto.Email;
        }

        if (dto.FirstName is not null) e.FirstName = dto.FirstName;
        if (dto.LastName  is not null) e.LastName  = dto.LastName;
        if (dto.HireDate  is not null) e.HireDate  = dto.HireDate.Value;
        if (dto.IsActive  is not null) e.IsActive  = dto.IsActive.Value;

        await db.SaveChangesAsync();
        return MapToRead(e);
    }

    // PUT (replace): volledige vervanging (EmployeeNo blijft bewust ongewijzigd)
    public async Task<EmployeeReadDto?> ReplaceAsync(Guid id, EmployeeReplaceDto dto)
    {
        var e = await db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return null;

        if (!string.Equals(dto.Email, e.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existsEmail = await db.Employees.IgnoreQueryFilters()
                                 .AnyAsync(x => x.Email == dto.Email);
            if (existsEmail) throw new ArgumentException($"Email '{dto.Email}' already exists.");
        }

        e.FirstName = dto.FirstName;
        e.LastName  = dto.LastName;
        e.Email     = dto.Email;
        e.HireDate  = dto.HireDate;
        e.IsActive  = dto.IsActive;

        await db.SaveChangesAsync();
        return MapToRead(e);
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var e = await db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return false;
        e.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    private static EmployeeReadDto MapToRead(Employee e) => new()
    {
        Id = e.Id,
        EmployeeNo = e.EmployeeNo,
        FirstName  = e.FirstName,
        LastName   = e.LastName,
        Email      = e.Email,
        HireDate   = e.HireDate,
        IsActive   = e.IsActive
    };
}

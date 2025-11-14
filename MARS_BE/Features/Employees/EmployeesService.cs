using MARS_BE.Common;
using MARS_BE.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MARS_BE.Common.Errors;
using MARS_BE.Common.Utils;

namespace MARS_BE.Features.Employees;

public sealed class EmployeesService : IEmployeesService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public EmployeesService(AppDbContext db, IMapper mapper)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }
    
    public async Task<PagedResult<EmployeeReadDto>> GetAllAsync(EmployeesQuery q)
    {
        var baseQ = _db.Employees.AsNoTracking().ApplyFilters(q);

        var total = await baseQ.CountAsync();

        var items = await baseQ
            .ApplySorting(q)
            .ApplyPaging(q.Page, q.PageSize)
            .ProjectTo<EmployeeReadDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<EmployeeReadDto>(items, q.Page, q.PageSize, total);
    }

    public async Task<EmployeeReadDto?> GetByIdAsync(Guid id, bool includeInactive)
    {
        var q = _db.Employees.AsNoTracking();
        if (includeInactive) q = q.IgnoreQueryFilters();

        var e = await q.FirstOrDefaultAsync(x => x.Id == id);
        return e is null ? null : _mapper.Map<EmployeeReadDto>(e);
    }

    public async Task<EmployeeReadDto> CreateAsync(EmployeeCreateDto dto)
    {
        // Uniqueness check on email (besides DB unique index, for a clean error)
        var existsEmail = await _db.Employees
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Email == dto.Email);

        if (existsEmail)
            throw new ConflictException($"Email '{dto.Email}' already exists.");

        var e = _mapper.Map<Employee>(dto);
        e.Id = Guid.NewGuid();
        e.IsActive = true;
        
        _db.Add(e);
        await _db.SaveChangesAsync();
        
        return _mapper.Map<EmployeeReadDto>(e);
    }

    // PATCH (partial update)
    public async Task<EmployeeReadDto?> UpdateAsync(Guid id, EmployeeUpdateDto dto)
    {
        var e = await _db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e is null) return null;
        
        // Handle email uniqueness separately (only when it actually changes)
        if (dto.Email is not null && 
            !string.Equals(dto.Email, e.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existsEmail = await _db.Employees
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Email == dto.Email);

            if (existsEmail)
                throw new ConflictException($"Email '{dto.Email}' already exists.");

            e.Email = dto.Email;
        }
        
        // Merge JSONB "Extras" values from DTO into current entity
        e.Extras.MergeFrom(dto.Extras);
        
        // Map remaining non-null fields from DTO onto the entity
        _mapper.Map(dto, e);
        await _db.SaveChangesAsync();
        
        return _mapper.Map<EmployeeReadDto>(e);
    }

    // PUT (replace): full replacement (EmployeeNo intentionally remains unchanged)
    public async Task<EmployeeReadDto?> ReplaceAsync(Guid id, EmployeeReplaceDto dto)
    {
        var e = await _db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e is null) return null;

        // Email uniqueness check if email is changed
        if (!string.Equals(dto.Email, e.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existsEmail = await _db.Employees
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Email == dto.Email);

            if (existsEmail)
                throw new ConflictException($"Email '{dto.Email}' already exists.");
        }

        // Map DTO onto entity (EmployeeNo is ignored in mapping profile)
        _mapper.Map(dto, e);
        await _db.SaveChangesAsync();

        return _mapper.Map<EmployeeReadDto>(e);
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var e = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return false;

        e.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }
}

using MARS_BE.Common;
using MARS_BE.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MARS_BE.Common.Errors;

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
        // Uniekheidscheck e-mail (naast DB unique index voor nette fout)
        var existsEmail = await _db.Employees.IgnoreQueryFilters()
                             .AnyAsync(x => x.Email == dto.Email);
        if (existsEmail) throw new ConflictException($"Email '{dto.Email}' already exists.");

        var e = _mapper.Map<Employee>(dto);
        e.Id = Guid.NewGuid();
        e.IsActive = true;
        
        _db.Add(e);
        await _db.SaveChangesAsync();
        
        return _mapper.Map<EmployeeReadDto>(e);
    }

    // PATCH (partial)
    public async Task<EmployeeReadDto?> UpdateAsync(Guid id, EmployeeUpdateDto dto)
    {
        var e = await _db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return null;

        if (dto.Email is not null && !string.Equals(dto.Email, e.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existsEmail = await _db.Employees.IgnoreQueryFilters()
                                 .AnyAsync(x => x.Email == dto.Email);
            if (existsEmail) throw new ConflictException($"Email '{dto.Email}' already exists.");
            e.Email = dto.Email;
        }
        
        _mapper.Map(dto, e);
        await _db.SaveChangesAsync();
        
        return _mapper.Map<EmployeeReadDto>(e);
    }

    // PUT (replace): volledige vervanging (EmployeeNo blijft bewust ongewijzigd)
    public async Task<EmployeeReadDto?> ReplaceAsync(Guid id, EmployeeReplaceDto dto)
    {
        var e = await _db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return null;

        if (!string.Equals(dto.Email, e.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existsEmail = await _db.Employees.IgnoreQueryFilters()
                                 .AnyAsync(x => x.Email == dto.Email);
            if (existsEmail) throw new ConflictException($"Email '{dto.Email}' already exists.");
        }

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

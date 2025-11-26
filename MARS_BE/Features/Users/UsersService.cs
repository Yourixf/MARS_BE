using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MARS_BE.Common.Utils;
using MARS_BE.Data;

namespace MARS_BE.Features.Users;

public interface IUsersService
{
    Task<PagedResult<UserReadDto>> GetAllAsync(UsersQuery q);
    Task<UserReadDto?> GetByIdAsync(Guid id, bool includeInactive);
    Task<UserReadDto> CreateAsync(UserCreateDto dto);
    Task<UserReadDto?> UpdateAsync(Guid id, UserUpdateDto dto);
    Task<bool> DeactivateAsync(Guid id);
}

public sealed class UsersService : IUsersService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersService(AppDbContext db, IMapper mapper, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<PagedResult<UserReadDto>> GetAllAsync(UsersQuery q)
    {
        var baseQ = _db.Users.AsNoTracking().ApplyFilters(q);

        var total = await baseQ.CountAsync();

        // ProjectTo keeps it efficient (SQL projection to DTO)
        var items = await baseQ
            .ApplySorting(q)
            .ApplyPaging(q.Page, q.PageSize)
            .ProjectTo<UserReadDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<UserReadDto>(items, q.Page, q.PageSize, total);
    }

    public async Task<UserReadDto?> GetByIdAsync(Guid id, bool includeInactive)
    {
        var q = _db.Users.AsNoTracking();
        if (includeInactive) q = q.IgnoreQueryFilters(); // if you add a global filter later

        var u = await q.FirstOrDefaultAsync(x => x.Id == id);
        return u is null ? null : _mapper.Map<UserReadDto>(u);
    }

    public async Task<UserReadDto> CreateAsync(UserCreateDto dto)
    {
        // Email uniqueness is already enforced by Identity + unique email policy;
        // we still check here for a clear error message.
        var exists = await _userManager.FindByEmailAsync(dto.Email);
        if (exists is not null)
            throw new InvalidOperationException($"Email '{dto.Email}' already exists.");

        var user = _mapper.Map<ApplicationUser>(dto);
        user.Id = Guid.NewGuid();

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create user: {msg}");
        }

        return _mapper.Map<UserReadDto>(user);
    }

    public async Task<UserReadDto?> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null) return null;

        // Email change: ensure uniqueness via UserManager rules (and sanity check here)
        if (dto.Email is not null && !string.Equals(dto.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _userManager.FindByEmailAsync(dto.Email);
            if (exists is not null && exists.Id != user.Id)
                throw new InvalidOperationException($"Email '{dto.Email}' already exists.");
        }

        _mapper.Map(dto, user);
        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded)
        {
            var msg = string.Join("; ", res.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not update user: {msg}");
        }

        return _mapper.Map<UserReadDto>(user);
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null) return false;

        user.IsActive = false;
        var res = await _userManager.UpdateAsync(user);
        return res.Succeeded;
    }
}
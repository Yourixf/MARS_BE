using MARS_BE.Common;

namespace MARS_BE.Features.Employees;

public interface IEmployeesService
{
    Task<PagedResult<EmployeeReadDto>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        bool includeInactive);
    Task<EmployeeReadDto?> GetByIdAsync(Guid id, bool includeInactive);
    Task<EmployeeReadDto> CreateAsync(EmployeeCreateDto dto);
    Task<EmployeeReadDto?> UpdateAsync(Guid id, EmployeeUpdateDto dto);
    Task<EmployeeReadDto?> ReplaceAsync(Guid id, EmployeeReplaceDto dto);
    Task<bool> SoftDeleteAsync(Guid id);
}
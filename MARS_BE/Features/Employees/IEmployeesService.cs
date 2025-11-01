using MARS_BE.Common;

namespace MARS_BE.Features.Employees;

public interface IEmployeesService
{
    Task<PagedResult<EmployeeReadDto>> GetAllAsync(EmployeesQuery q);
    Task<EmployeeReadDto?> GetByIdAsync(Guid id, bool includeInactive);
    Task<EmployeeReadDto> CreateAsync(EmployeeCreateDto dto);
    Task<EmployeeReadDto?> UpdateAsync(Guid id, EmployeeUpdateDto dto);
    Task<EmployeeReadDto?> ReplaceAsync(Guid id, EmployeeReplaceDto dto);
    Task<bool> SoftDeleteAsync(Guid id);
}
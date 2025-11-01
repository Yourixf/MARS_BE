using AutoMapper;
using MARS_BE.Features.Employees;

namespace MARS_BE.Common.Mapping;

public sealed class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        // Entity -> Read DTO
        CreateMap<Employee, EmployeeReadDto>();

        // Create DTO -> Entity
        CreateMap<EmployeeCreateDto, Employee>()
            .ForMember(d => d.Id, o => o.Ignore())   // zetten we in service
            .ForMember(d => d.IsActive, o => o.Ignore());  // idem

        // PATCH (partial): map alleen niet-null waarden
        CreateMap<EmployeeUpdateDto, Employee>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember is not null));

        // PUT (replace): volledige vervanging, maar laat EmployeeNo met rust (immutable)
        CreateMap<EmployeeReplaceDto, Employee>()
            .ForMember(d => d.EmployeeNo, o => o.Ignore()); // immuut
    }
}
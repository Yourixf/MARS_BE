using AutoMapper;

namespace MARS_BE.Features.Employees;

public sealed class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        // Entity -> Read DTO
        CreateMap<Employee, EmployeeReadDto>();

        // Create DTO -> Entity
        CreateMap<EmployeeCreateDto, Employee>()
            // We place this in the service
            .ForMember(d => d.Id, o => o.Ignore())  
            // same
            .ForMember(d => d.IsActive, o => o.Ignore())
            .ForMember(d => d.Extras,   o => 
                o.MapFrom(s => s.Extras ?? new Dictionary<string, object?>()));


        // PATCH (partial): map only non null values
        CreateMap<EmployeeUpdateDto, Employee>()
            .ForAllMembers(opt => 
                opt.Condition((src, dest, srcMember) => srcMember is not null));

        // PUT (replace): full replacement, skip immutable EmployeeNo
        CreateMap<EmployeeReplaceDto, Employee>()
            .ForMember(d => d.EmployeeNo, o => o.Ignore());
    }
}
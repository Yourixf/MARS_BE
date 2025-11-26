using AutoMapper;

namespace MARS_BE.Features.Users;

public sealed class UserProfile : Profile
{
    public UserProfile()
    {
        // Entity -> Read DTO
        CreateMap<ApplicationUser, UserReadDto>();

        // Create DTO -> Entity (password is handled by UserManager)
        CreateMap<UserCreateDto, ApplicationUser>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.Email))
            .ForMember(d => d.EmailConfirmed, o => o.MapFrom(_ => true))
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));

        // PATCH: map only provided (non-null) values
        CreateMap<UserUpdateDto, ApplicationUser>()
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember is not null));
    }
}
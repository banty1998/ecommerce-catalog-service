using AuthAPI.DTOs;
using AuthAPI.Models;
using AutoMapper;

namespace AuthAPI.Profiles
{
    public class AuthProfile : Profile
    {
        public AuthProfile()
        {
            // Maps the internal user to the response DTO. 
            // We ignore the Token property here because we generate it manually in the service.
            CreateMap<ApplicationUser, LoginResponseDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Token, opt => opt.Ignore());
            // Add this inside the constructor of your AuthProfile
            CreateMap<RegisterRequestDto, ApplicationUser>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid().ToString()))
                // We ignore PasswordHash because we will map it manually after hashing
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        }
    }
}

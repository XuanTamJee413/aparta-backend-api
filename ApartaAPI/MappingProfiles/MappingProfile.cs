using AutoMapper;
using ApartaAPI.Models;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.DTOs.Auth;

namespace ApartaAPI.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Project, ProjectDto>();

            CreateMap<ProjectCreateDto, Project>();

            CreateMap<ProjectUpdateDto, Project>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
            
            CreateMap<ProfileUpdateDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Phone, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<User, UserInfoResponse>()
                .ForMember(dest => dest.Role, opt => opt.Ignore());
        }
    }
}
using AutoMapper;
using ApartaAPI.Models;
using ApartaAPI.DTOs.Projects;

namespace ApartaAPI.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Project, ProjectDto>();

            CreateMap<ProjectCreateDto, Project>();

            // Only map non-null values on update
            CreateMap<ProjectUpdateDto, Project>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
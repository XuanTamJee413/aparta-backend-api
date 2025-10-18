using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;
using AutoMapper;

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

            CreateMap<ApartmentMember, ApartmentMemberDto>();
            CreateMap<ApartmentMemberCreateDto, ApartmentMember>();
            CreateMap<ApartmentMemberUpdateDto, ApartmentMember>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Visitor, VisitorDto>();
            CreateMap<VisitorCreateDto, Visitor>();
            CreateMap<VisitorUpdateDto, Visitor>()
              .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<VisitLog, VisitLogDto>();
            CreateMap<VisitLogCreateDto, VisitLog>();
            CreateMap<VisitLogUpdateDto, VisitLog>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
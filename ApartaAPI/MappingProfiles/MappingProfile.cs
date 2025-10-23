using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;
using AutoMapper;
﻿using AutoMapper;
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
                .ForMember(dest => dest.ProjectCode, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
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

            CreateMap<VisitLog, VisitLogHistoryDto>()
                .ForMember(dest => dest.VisitorName, opt => opt.MapFrom(src => src.Visitor.FullName));

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
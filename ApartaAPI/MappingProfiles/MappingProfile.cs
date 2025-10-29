using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Assets;
using ApartaAPI.DTOs.Auth;
using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.News;
using ApartaAPI.DTOs.PriceQuotations;
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

            CreateMap<ProjectUpdateDto, Project>()
                .ForMember(dest => dest.ProjectCode, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Building, BuildingDto>();

            CreateMap<BuildingCreateDto, Building>()
                .ForMember(dest => dest.BuildingId, opt => opt.Ignore());

            CreateMap<BuildingUpdateDto, Building>()
                .ForMember(dest => dest.BuildingCode, opt => opt.Ignore())
                .ForMember(dest => dest.BuildingId, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ApartmentMember, ApartmentMemberDto>();
            CreateMap<ApartmentMemberCreateDto, ApartmentMember>();
            CreateMap<ApartmentMemberUpdateDto, ApartmentMember>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<VisitLog, VisitLogStaffViewDto>()
                .ForMember(
                    dest => dest.ApartmentCode,
                    opt => opt.MapFrom(src => src.Apartment.Code)
                )
                .ForMember(
                    dest => dest.VisitorFullName,
                    opt => opt.MapFrom(src => src.Visitor.FullName)
                )
                .ForMember(
                    dest => dest.VisitorIdNumber,
                    opt => opt.MapFrom(src => src.Visitor.IdNumber)
                );
            CreateMap<VisitorCreateDto, Visitor>();
            CreateMap<VisitorCreateDto, VisitLog>()
                .ForMember(dest => dest.VisitorId, opt => opt.Ignore())
                .ForMember(dest => dest.CheckinTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());
            CreateMap<Visitor, VisitorDto>();

            CreateMap<PriceQuotation, PriceQuotationDto>()
                .ForMember(dest => dest.BuildingCode,
                    opt => opt.MapFrom(src => src.Building != null ? src.Building.BuildingCode : null)
                );
            CreateMap<PriceQuotationCreateDto, PriceQuotation>();

            CreateMap<ProfileUpdateDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Phone, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<User, UserInfoResponse>()
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            CreateMap<User, ManagerDto>()
                .ForMember(dest => dest.Role,
                    opt => opt.MapFrom(src => src.Role.RoleName))
                .ForMember(dest => dest.PermissionGroup,
                    opt => opt.Ignore())
                .ForMember(dest => dest.AvatarUrl,
                    opt => opt.MapFrom(src => src.AvatarUrl))
                .ForMember(dest => dest.Email,
                    opt => opt.MapFrom(src => src.Email))
                .ForAllMembers(opt => opt.Condition((src, dest,
                    srcMember) => srcMember != null));

            // News mappings
            CreateMap<News, NewsDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.AuthorUser.Name));
            CreateMap<CreateNewsDto, News>();
            CreateMap<UpdateNewsDto, News>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));


            CreateMap<Asset, AssetDto>();
            CreateMap<AssetCreateDto, Asset>();
            CreateMap<AssetUpdateDto, Asset>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
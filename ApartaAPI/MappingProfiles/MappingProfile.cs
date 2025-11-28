using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Apartments;
using ApartaAPI.DTOs.Apartments;
using ApartaAPI.DTOs.Assets;
using ApartaAPI.DTOs.Auth;
using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.Contracts;
using ApartaAPI.DTOs.Invoices;
using ApartaAPI.DTOs.MeterReadings;
using ApartaAPI.DTOs.News;
using ApartaAPI.DTOs.PriceQuotations;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.DTOs.Proposals;
using ApartaAPI.DTOs.Roles;
using ApartaAPI.DTOs.Subscriptions;
using ApartaAPI.DTOs.Vehicles;
using ApartaAPI.DTOs.Vehicles;
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

            CreateMap<Subscription, SubscriptionDto>();

            CreateMap<SubscriptionCreateOrUpdateDto, Subscription>()
                .ForMember(dest => dest.SubscriptionId, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId))
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ExpiredAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.SubscriptionCode, opt => opt.MapFrom(src => src.SubscriptionCode))
                .ForMember(dest => dest.NumMonths, opt => opt.MapFrom(src => src.NumMonths))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.AmountPaid, opt => opt.MapFrom(src => src.AmountPaid))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod))
                .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.PaymentDate))
                .ForMember(dest => dest.PaymentNote, opt => opt.MapFrom(src => src.PaymentNote))
                .ForMember(dest => dest.Tax, opt => opt.Ignore())
                .ForMember(dest => dest.Discount, opt => opt.Ignore());

            CreateMap<Role, RoleDto>();
            CreateMap<Permission, PermissionDto>()
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.PermissionGroup.Name));

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

            // Proposal Mappings
            CreateMap<Proposal, ProposalDto>()
                .ForMember(dest => dest.ResidentName, opt => opt.MapFrom(src => src.Resident.Name))
                .ForMember(dest => dest.OperationStaffName, opt => opt.MapFrom(src => src.OperationStaff != null ? src.OperationStaff.Name : "Chưa được gán"));

            CreateMap<ProposalCreateDto, Proposal>()
                .ForMember(dest => dest.ProposalId, opt => opt.Ignore())
                .ForMember(dest => dest.ResidentId, opt => opt.Ignore())
                .ForMember(dest => dest.OperationStaffId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

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

            CreateMap<Vehicle, VehicleDto>();
            CreateMap<VehicleCreateDto, Vehicle>();
            CreateMap<VehicleUpdateDto, Vehicle>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Apartment, ApartmentDto>();
            CreateMap<ApartmentCreateDto, Apartment>();
            CreateMap<ApartmentUpdateDto, Apartment>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // Invoice mappings
            CreateMap<Invoice, InvoiceDto>()
                .ForMember(dest => dest.ApartmentCode, opt => opt.MapFrom(src => src.Apartment.Code))
                .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.Staff != null ? src.Staff.Name : null));

            CreateMap<InvoiceItem, InvoiceItemDto>();

            CreateMap<Invoice, InvoiceDetailDto>()
                .ForMember(dest => dest.ApartmentCode, opt => opt.MapFrom(src => src.Apartment.Code))
                .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.Staff != null ? src.Staff.Name : null))
                .ForMember(dest => dest.ResidentName, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.InvoiceItems.OrderBy(i => i.CreatedAt)))
                .AfterMap((src, dest) =>
                {
                    var resident = src.Apartment.Users
                        .FirstOrDefault(u => u.Role.RoleName.ToLower() == "resident");
                    dest.ResidentName = resident != null 
                        ? resident.Name 
                        : src.Apartment.Users.FirstOrDefault()?.Name;
                });
            
            CreateMap<PriceQuotation, InvoiceItem>()
                .ForMember(dest => dest.InvoiceItemId, opt => opt.Ignore())
                .ForMember(dest => dest.InvoiceId, opt => opt.Ignore())
                .ForMember(dest => dest.FeeType, opt => opt.MapFrom(src => src.FeeType))
                .ForMember(dest => dest.Description, opt => opt.Ignore())
                .ForMember(dest => dest.Quantity, opt => opt.Ignore())
                .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
                .ForMember(dest => dest.Total, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Invoice, opt => opt.Ignore());

            // MeterReading mappings
            CreateMap<MeterReading, MeterReadingDto>();

            CreateMap<MeterReadingCreateDto, MeterReading>();



            CreateMap<Contract, ContractDto>();
            CreateMap<ContractCreateDto, Contract>();
            CreateMap<ContractUpdateDto, Contract>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        }
    }
}
using System.Security.Claims;

namespace ApartaAPI.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // === CÁC POLICY CỐ ĐỊNH ===
                // 1. Chỉ Admin
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("admin"));

                // 2. Manager và Admin
                options.AddPolicy("ManagerAccess", policy =>
                    policy.RequireRole("admin", "manager"));

                // 3. Cư dân (và các cấp quản lý)
                options.AddPolicy("ResidentAccess", policy =>
                    policy.RequireRole("admin", "manager", "resident"));

                // === POLICY DỰA TRÊN PERMISSION ===

                // Policy cho Project
                options.AddPolicy("CanReadProject", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "project.read")
                ));

                options.AddPolicy("CanCreateProject", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "project.create")
                ));

                options.AddPolicy("CanUpdateProject", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "project.update")
                ));

                // Policy cho Asset
                options.AddPolicy("CanReadAsset", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "asset.read")
                ));

                options.AddPolicy("CanCreateAsset", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "asset.create")
                ));

                options.AddPolicy("CanUpdateAsset", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "asset.update")
                ));

                options.AddPolicy("CanDeleteAsset", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "asset.delete")
                ));

                // Policy cho News
                options.AddPolicy("CanReadNews", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "news.read")
                ));

                options.AddPolicy("CanCreateNews", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "news.create")
                ));

                options.AddPolicy("CanUpdateNews", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "news.update")
                ));

                options.AddPolicy("CanDeleteNews", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "news.delete")
                ));

                // Policy cho Building
                options.AddPolicy("CanReadBuilding", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "building.read")
                ));

                options.AddPolicy("CanCreateBuilding", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "building.create")
                ));

                options.AddPolicy("CanUpdateBuilding", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "building.update")
                ));

                // Policy cho Service
                options.AddPolicy("CanReadService", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "service.read")
                ));

                options.AddPolicy("CanCreateService", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "service.create")
                ));

                options.AddPolicy("CanUpdateService", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "service.update")
                ));

                options.AddPolicy("CanDeleteService", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "service.delete")
                ));

                // Policy cho Utility
                options.AddPolicy("CanReadUtility", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "utility.read")
                ));

                options.AddPolicy("CanCreateUtility", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "utility.create")
                ));

                options.AddPolicy("CanUpdateUtility", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "utility.update")
                ));

                options.AddPolicy("CanDeleteUtility", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "utility.delete")
                ));

                // Policy cho Visitor và VisitLog
                options.AddPolicy("CanReadVisitor", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "visitor.read")
                ));

                options.AddPolicy("CanCreateVisitor", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "visitor.create")
                ));

                options.AddPolicy("CanCheckInVisitor", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "visitor.checkin")
                ));

                options.AddPolicy("CanCheckOutVisitor", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "visitor.checkout")
                ));

                // Policy cho ApartmentMember
                options.AddPolicy("CanReadMember", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("manager") ||
                    ctx.User.HasClaim("permission", "apartmentmember.read")
                ));

                options.AddPolicy("CanCreateMember", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "apartmentmember.create")
                ));

                options.AddPolicy("CanUpdateMember", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "apartmentmember.update")
                ));

                options.AddPolicy("CanDeleteMember", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "apartmentmember.delete")
                ));

                // Policy cho MeterReading
                options.AddPolicy("CanReadMeterReadings", policy =>
                    policy.RequireClaim("Permission", "meterreading.read")); 

                options.AddPolicy("CanCreateMeterReadings", policy =>
                    policy.RequireClaim("Permission", "meterreading.create"));

                options.AddPolicy("CanUpdateMeterReadings", policy =>
                    policy.RequireClaim("Permission", "meterreading.update"));

                options.AddPolicy("CanReadMeterReadingStatus", policy =>
                    policy.RequireClaim("Permission", "meterreading.read.status"));
                // Policy cho Invoice
                options.AddPolicy("CanCreateInvoicePayment", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("resident") ||
                    ctx.User.HasClaim("permission", "invoice.pay.create")
                ));

                options.AddPolicy("CanReadInvoiceItem", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("manager") ||
                    ctx.User.HasClaim("permission", "invoice.item.read")
                ));

                options.AddPolicy("CanReadInvoiceResident", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("resident") ||
                    ctx.User.HasClaim("permission", "invoice.resident.read")
                ));

                // Policy cho Billing 
                options.AddPolicy("CanGenerateBilling", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "billing.generate")
                ));

                // Policy cho User by Apartment
                options.AddPolicy("CanReadUserByApartment", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("manager") ||
                    ctx.User.HasClaim("permission", "user.read.byapartment")
                ));

                // Policy cho StaffAssignment  
                options.AddPolicy("CanReadStaffAssignment", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("manager") ||
                    ctx.User.HasClaim("permission", "staffassignment.read")
                ));

                options.AddPolicy("CanCreateStaffAssignment", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("manager") ||
                    ctx.User.HasClaim("permission", "staffassignment.create")
                ));

                options.AddPolicy("CanUpdateStaffAssignment", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("manager") ||
                    ctx.User.HasClaim("permission", "staffassignment.update")
                ));

                // Policy cho User Management
                options.AddPolicy("CanManageStaff", policy => policy.RequireRole("admin")); // Chỉ Admin quản lý nhân viên
                options.AddPolicy("CanViewResidents", policy => policy.RequireRole("admin", "manager")); // Admin & Manager xem cư dân
                options.AddPolicy("CanUpdateUserStatus", policy => policy.RequireRole("admin", "manager"));

                // Policy cho Contract 
                options.AddPolicy("CanReadContract", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("manager") ||
                    ctx.User.HasClaim("permission", "contract.read")
                ));

                options.AddPolicy("CanCreateContract", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("manager") ||
                    ctx.User.HasClaim("permission", "contract.create")
                ));

                options.AddPolicy("CanUpdateContract", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.IsInRole("manager") ||
                    ctx.User.HasClaim("permission", "contract.update")
                ));

                options.AddPolicy("CanDeleteContract", policy => policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("admin") ||
                    ctx.User.HasClaim("permission", "contract.delete")
                ));
            });

            return services;
        }
    }
}
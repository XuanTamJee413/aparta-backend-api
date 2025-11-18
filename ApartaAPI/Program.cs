using ApartaAPI.BackgroundJobs;
using ApartaAPI.Data;
using ApartaAPI.Extensions;
using ApartaAPI.Profiles;
using ApartaAPI.Repositories;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;
using QuestPDF.Infrastructure;

namespace ApartaAPI
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

            QuestPDF.Settings.License = LicenseType.Community;
            var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

			builder.Services.AddCors(options =>
			{
				options.AddPolicy(name: myAllowSpecificOrigins,
					policy =>
					{
						policy.WithOrigins("http://localhost:4200")
							  .AllowAnyHeader()
							  .AllowAnyMethod();
					});
			});

			// Add services to the container.
			builder.Services.AddControllers();

			builder.Services.AddDbContext<ApartaDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			// AutoMapper
			builder.Services.AddAutoMapper(typeof(Program).Assembly);

			// Repositories & Services
			builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
			
			// Services
			builder.Services.AddScoped<IProjectService, ProjectService>();
			builder.Services.AddScoped<IAuthService, AuthService>();
			builder.Services.AddScoped<IServiceService, ServiceService>();
			builder.Services.AddScoped<IServiceBookingService, ServiceBookingService>();
			builder.Services.AddScoped<IUtilityService, UtilityService>();
			builder.Services.AddScoped<IBuildingService, BuildingService>();
			builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
			builder.Services.AddHostedService<SubscriptionExpiryService>();
			builder.Services.AddScoped<IRoleService, RoleService>();
			builder.Services.AddScoped<IApartmentMemberService, ApartmentMemberService>();
			builder.Services.AddScoped<IVisitorService, VisitorService>();
			builder.Services.AddScoped<IVisitLogService, VisitLogService>();
			builder.Services.AddScoped<IAssetService, AssetService>();
			builder.Services.AddScoped<IManagerService, ManagerService>();
			builder.Services.AddScoped<INewsService, NewsService>();
			builder.Services.AddScoped<IPriceQuotationService, PriceQuotationService>();
			builder.Services.AddScoped<IInvoiceService, InvoiceService>();
			builder.Services.AddScoped<IVehicleService, VehicleService>();
			builder.Services.AddScoped<IApartmentService, ApartmentService>();
			builder.Services.AddScoped<IMeterReadingService, MeterReadingService>();
			builder.Services.AddScoped<IContractService, ContractService>();
            builder.Services.AddScoped<IContractPdfService, ContractPdfService>();
            builder.Services.AddSingleton<PayOSService>();
			
			// Custom Repositories
			builder.Services.AddScoped<IVisitLogRepository, VisitLogRepository>();
			builder.Services.AddScoped<IVisitorRepository, VisitorRepository>();
			builder.Services.AddScoped<IPriceQuotationRepository, PriceQuotationRepository>();

			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(options =>
			{
				// 1. Định nghĩa Security Scheme (Cách Swagger biết về Authentication)
				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Name = "Authorization", // Tên của header
					Type = SecuritySchemeType.Http, // Kiểu
					Scheme = "Bearer", // Lược đồ (Bearer)
					BearerFormat = "JWT", // Định dạng
					In = ParameterLocation.Header, // Nơi đặt token (trong Header)
					Description = "Please enter your JWT token. Example: \"Bearer {token}\""
				});

				// 2. Thêm Security Requirement (Áp dụng scheme này cho các API)
				options.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = "Bearer" // Phải khớp với tên đã định nghĩa ở trên
							}
						},
						new string[] {} // Không yêu cầu scopes cụ thể
					}
				});
			});

			// JWT Authentication
			var jwtSettings = builder.Configuration.GetSection("JwtSettings");
			var secretKey = jwtSettings["SecretKey"] ?? "14da3d232e7472b1197c6262937d1aaa49cdc1acc71db952e6aed7f40df50ad6";
			var issuer = jwtSettings["Issuer"] ?? "ApartaAPI";
			var audience = jwtSettings["Audience"] ?? "ApartaAPI";

			builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidIssuer = issuer,
						ValidAudience = audience,
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
						ClockSkew = TimeSpan.Zero
					};
				});

			builder.Services.AddAuthorizationPolicies();

			var app = builder.Build();

			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseRouting();

			app.UseCors(myAllowSpecificOrigins);

			app.UseAuthentication();

			app.UseAuthorization();

			app.MapControllers();
			app.Run();
		}
	}
}
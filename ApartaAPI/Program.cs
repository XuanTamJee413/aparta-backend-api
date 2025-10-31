using ApartaAPI.BackgroundJobs;
using ApartaAPI.Data;
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
using System;
using System.Text;

namespace ApartaAPI
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

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
			builder.Services.AddScoped<IProjectService, ProjectService>();
			builder.Services.AddScoped<IAuthService, AuthService>();
			builder.Services.AddScoped<IServiceService, ServiceService>();
			builder.Services.AddScoped<IUtilityService, UtilityService>();
			builder.Services.AddScoped<IBuildingService, BuildingService>();
			builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
			builder.Services.AddHostedService<SubscriptionExpiryService>();

			builder.Services.AddScoped<IApartmentMemberService, ApartmentMemberService>();
			builder.Services.AddScoped<IVisitorService, VisitorService>();
			builder.Services.AddScoped<IVisitLogService, VisitLogService>();
			builder.Services.AddScoped<IAssetService, AssetService>();
            builder.Services.AddScoped<IVisitLogRepository, VisitLogRepository>();
            builder.Services.AddScoped<IVisitorRepository, VisitorRepository>();
            builder.Services.AddScoped<IManagerService, ManagerService>();
            builder.Services.AddScoped<INewsService, NewsService>();
            builder.Services.AddScoped<IPriceQuotationRepository, PriceQuotationRepository>();
            builder.Services.AddScoped<IPriceQuotationService, PriceQuotationService>();
            builder.Services.AddScoped<IMeterReadingRepository, MeterReadingRepository>();
            builder.Services.AddScoped<IMeterReadingService, MeterReadingService>();
            builder.Services.AddScoped<IInvoiceService, InvoiceService>();
            builder.Services.AddSingleton<PayOSService>();
            builder.Services.AddEndpointsApiExplorer();

            // Repositories & Services
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IManagerService, ManagerService>();
            builder.Services.AddScoped<INewsService, NewsService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


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

			// Authorization Policies
			builder.Services.AddAuthorization(options =>
			{
				// Admin policy - only admin role
				options.AddPolicy("AdminOnly", policy =>
					policy.RequireRole("admin"));

				// Staff policy - admin and staff roles
				options.AddPolicy("StaffOrAdmin", policy =>
					policy.RequireRole("admin", "staff"));

				// Resident policy - admin, staff, and resident roles
				options.AddPolicy("ResidentOrAbove", policy =>
					policy.RequireRole("admin", "staff", "resident"));

				// Specific role policies
				options.AddPolicy("AdminPolicy", policy =>
					policy.RequireRole("admin"));

				options.AddPolicy("StaffPolicy", policy =>
					policy.RequireRole("staff"));

				options.AddPolicy("ResidentPolicy", policy =>
					policy.RequireRole("resident"));
			});

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
using ApartaAPI.Data;
using ApartaAPI.Profiles;
using ApartaAPI.Repositories;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System;

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
            builder.Services.AddScoped<IBuildingService, BuildingService>();

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

            builder.Services.AddScoped<IApartmentMemberService, ApartmentMemberService>();
            builder.Services.AddScoped<IVisitorService, VisitorService>();
            builder.Services.AddScoped<IVisitLogService, VisitLogService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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

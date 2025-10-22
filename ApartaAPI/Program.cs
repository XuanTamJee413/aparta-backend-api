using ApartaAPI.Data;
using ApartaAPI.Profiles;
using ApartaAPI.Repositories;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

            builder.Services.AddScoped<IApartmentMemberService, ApartmentMemberService>();
            builder.Services.AddScoped<IVisitorService, VisitorService>();
            builder.Services.AddScoped<IVisitLogService, VisitLogService>();
            builder.Services.AddScoped<IAssetService, AssetService>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors(myAllowSpecificOrigins);

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}

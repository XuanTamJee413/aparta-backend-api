using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Repositories
{
    public class VisitLogRepositoryTests
    {
        private readonly ApartaDbContext _context;
        private readonly VisitLogRepository _repository;

        public VisitLogRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);
            _repository = new VisitLogRepository(_context);
        }

        [Fact]
        public async Task GetStaffViewLogsQuery_KhiGoi_NenTraVeQueryVoiIncludes()
        {

            // Arrange
            var buildingId = "b1";
            var building = new Building { BuildingId = buildingId, BuildingCode = "B1", Name = "Building 1", ProjectId = "proj1" };
            var apartment = new Apartment { ApartmentId = "apt1", BuildingId = buildingId, Code = "A101", Status = "Active" };
            var visitor = new Visitor { VisitorId = "v1", FullName = "John Doe", IdNumber = "123", Phone = "123" };
            
            var log1 = new VisitLog { VisitLogId = "vl1", VisitorId = "v1", ApartmentId = "apt1", Status = "Pending", Visitor = visitor, Apartment = apartment };
            var log2 = new VisitLog { VisitLogId = "vl2", VisitorId = "v1", ApartmentId = "apt1", Status = "Deleted", Visitor = visitor, Apartment = apartment };

            await _context.Buildings.AddAsync(building);
            await _context.Apartments.AddAsync(apartment);
            await _context.Visitors.AddAsync(visitor);
            await _context.VisitLogs.AddRangeAsync(log1, log2);
            await _context.SaveChangesAsync();

            // Act
            var query = _repository.GetStaffViewLogsQuery();
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
            var log = result.First();
            log.VisitLogId.Should().Be("vl1");
            log.Visitor.Should().NotBeNull();
            log.Apartment.Should().NotBeNull();
        }
    }
}

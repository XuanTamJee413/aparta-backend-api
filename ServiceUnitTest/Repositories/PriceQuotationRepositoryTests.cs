using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Repositories
{
    public class PriceQuotationRepositoryTests
    {
        private readonly ApartaDbContext _context;
        private readonly PriceQuotationRepository _repository;

        public PriceQuotationRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);
            _repository = new PriceQuotationRepository(_context);
        }

        [Fact]
        public async Task GetAllWithBuildingAsync_KhiGoi_NenTraVeDanhSachVoiBuilding()
        {
            // Arrange
            var building = new Building { BuildingId = "b1", BuildingCode = "B1", Name = "Building 1", ProjectId = "proj1" };
            var pq1 = new PriceQuotation { PriceQuotationId = "pq1", BuildingId = "b1", FeeType = "Type1", Building = building, CalculationMethod = "Method" };
            var pq2 = new PriceQuotation { PriceQuotationId = "pq2", BuildingId = "b1", FeeType = "Type2", Building = building, CalculationMethod = "Method" };

            await _context.Buildings.AddAsync(building);
            await _context.PriceQuotations.AddRangeAsync(pq1, pq2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllWithBuildingAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().Building.Should().NotBeNull();
            result.First().Building!.BuildingCode.Should().Be("B1");
        }

        [Fact]
        public async Task GetByBuildingIdWithBuildingAsync_KhiBuildingIdTonTai_NenTraVeDanhSach()
        {
            // Arrange
            var building = new Building { BuildingId = "b1", BuildingCode = "B1", Name = "Building 1", ProjectId = "proj1" };
            var pq1 = new PriceQuotation { PriceQuotationId = "pq1", BuildingId = "b1", FeeType = "Type1", Building = building, CalculationMethod = "Method" };
            var pq2 = new PriceQuotation { PriceQuotationId = "pq2", BuildingId = "other", FeeType = "Type2", CalculationMethod = "Method" };

            await _context.Buildings.AddAsync(building);
            await _context.PriceQuotations.AddRangeAsync(pq1, pq2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByBuildingIdWithBuildingAsync("b1");

            // Assert
            result.Should().HaveCount(1);
            result.First().PriceQuotationId.Should().Be("pq1");
            result.First().Building.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdWithBuildingAsync_KhiIdTonTai_NenTraVeChiTiet()
        {
            // Arrange
            var building = new Building { BuildingId = "b1", BuildingCode = "B1", Name = "Building 1", ProjectId = "proj1" };
            var pq1 = new PriceQuotation { PriceQuotationId = "pq1", BuildingId = "b1", FeeType = "Type1", Building = building, CalculationMethod = "Method" };

            await _context.Buildings.AddAsync(building);
            await _context.PriceQuotations.AddAsync(pq1);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdWithBuildingAsync("pq1");

            // Assert
            result.Should().NotBeNull();
            result!.PriceQuotationId.Should().Be("pq1");
            result.Building.Should().NotBeNull();
        }

        [Fact]
        public void GetQuotationsQueryable_KhiGoi_NenTraVeQueryable()
        {
            // Act
            var query = _repository.GetQuotationsQueryable();

            // Assert
            query.Should().NotBeNull();
            query.Should().BeAssignableTo<IQueryable<PriceQuotation>>();
        }
    }
}

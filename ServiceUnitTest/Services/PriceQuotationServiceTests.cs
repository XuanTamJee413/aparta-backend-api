using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.PriceQuotations;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using AutoMapper;
using FluentAssertions;
using Moq;
using ServiceUnitTest.Helpers;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Services
{
    public class PriceQuotationServiceTests
    {
        private readonly Mock<IPriceQuotationRepository> _mockRepo;
        private readonly Mock<IRepository<Building>> _mockBuildingRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly PriceQuotationService _service;

        public PriceQuotationServiceTests()
        {
            _mockRepo = new Mock<IPriceQuotationRepository>();
            _mockBuildingRepo = new Mock<IRepository<Building>>();
            _mockMapper = new Mock<IMapper>();

            _service = new PriceQuotationService(
                _mockRepo.Object,
                _mockBuildingRepo.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task GetPriceQuotationsAsync_KhiGoi_NenTraVeDanhSach()
        {
            // Arrange
            var quotations = new List<PriceQuotation> { new PriceQuotation { PriceQuotationId = "pq1" } };
            _mockRepo.Setup(r => r.GetAllWithBuildingAsync()).ReturnsAsync(quotations);
            _mockMapper.Setup(m => m.Map<IEnumerable<PriceQuotationDto>>(quotations)).Returns(new List<PriceQuotationDto> { new PriceQuotationDto { PriceQuotationId = "pq1" } });

            // Act
            var result = await _service.GetPriceQuotationsAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().PriceQuotationId.Should().Be("pq1");
        }

        [Fact]
        public async Task CreatePriceQuotationAsync_KhiHopLe_NenTaoMoi()
        {
            // Arrange
            var createDto = new PriceQuotationCreateDto { BuildingId = "b1", FeeType = "Type1" };
            var building = new Building { BuildingId = "b1", BuildingCode = "B1" };
            var quotation = new PriceQuotation { PriceQuotationId = "pq1" };

            _mockBuildingRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Building, bool>>>()))
                .ReturnsAsync(building);
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PriceQuotation, bool>>>()))
                .ReturnsAsync((PriceQuotation?)null);
            _mockMapper.Setup(m => m.Map<PriceQuotation>(createDto)).Returns(quotation);
            _mockMapper.Setup(m => m.Map<PriceQuotationDto>(quotation)).Returns(new PriceQuotationDto { PriceQuotationId = "pq1" });

            // Act
            var result = await _service.CreatePriceQuotationAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result!.PriceQuotationId.Should().Be("pq1");
            result.BuildingCode.Should().Be("B1");
            _mockRepo.Verify(r => r.AddAsync(quotation), Times.Once);
        }

        [Fact]
        public async Task CreatePriceQuotationAsync_KhiBuildingKhongTonTai_NenTraVeNull()
        {
            // Arrange
            var createDto = new PriceQuotationCreateDto { BuildingId = "invalid" };
            _mockBuildingRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Building, bool>>>()))
                .ReturnsAsync((Building?)null);

            // Act
            var result = await _service.CreatePriceQuotationAsync(createDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreatePriceQuotationAsync_KhiFeeTypeTrung_NenNemInvalidOperationException()
        {
            // Arrange
            var createDto = new PriceQuotationCreateDto { BuildingId = "b1", FeeType = "Type1" };
            var building = new Building { BuildingId = "b1" };
            var existing = new PriceQuotation { PriceQuotationId = "pq1" };

            _mockBuildingRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Building, bool>>>()))
                .ReturnsAsync(building);
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PriceQuotation, bool>>>()))
                .ReturnsAsync(existing);

            // Act
            Func<Task> act = async () => await _service.CreatePriceQuotationAsync(createDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã tồn tại*");
        }

        [Fact]
        public async Task GetPriceQuotationsByBuildingIdAsync_KhiBuildingTonTai_NenTraVeDanhSach()
        {
            // Arrange
            var buildingId = "b1";
            var building = new Building { BuildingId = buildingId };
            var quotations = new List<PriceQuotation> { new PriceQuotation { PriceQuotationId = "pq1" } };

            _mockBuildingRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Building, bool>>>()))
                .ReturnsAsync(building);
            _mockRepo.Setup(r => r.GetByBuildingIdWithBuildingAsync(buildingId)).ReturnsAsync(quotations);
            _mockMapper.Setup(m => m.Map<IEnumerable<PriceQuotationDto>>(quotations)).Returns(new List<PriceQuotationDto> { new PriceQuotationDto { PriceQuotationId = "pq1" } });

            // Act
            var result = await _service.GetPriceQuotationsByBuildingIdAsync(buildingId);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetPriceQuotationByIdAsync_KhiIdTonTai_NenTraVeChiTiet()
        {
            // Arrange
            var id = "pq1";
            var quotation = new PriceQuotation { PriceQuotationId = id };
            _mockRepo.Setup(r => r.GetByIdWithBuildingAsync(id)).ReturnsAsync(quotation);
            _mockMapper.Setup(m => m.Map<PriceQuotationDto>(quotation)).Returns(new PriceQuotationDto { PriceQuotationId = id });

            // Act
            var result = await _service.GetPriceQuotationByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result!.PriceQuotationId.Should().Be(id);
        }

        [Fact]
        public async Task UpdateAsync_KhiIdTonTaiVaHopLe_NenCapNhat()
        {
            // Arrange
            var id = "pq1";
            var updateDto = new PriceQuotationCreateDto { FeeType = "NewType" };
            var entity = new PriceQuotation { PriceQuotationId = id, FeeType = "OldType", BuildingId = "b1" };

            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PriceQuotation, bool>>>()))
                .ReturnsAsync((System.Linq.Expressions.Expression<Func<PriceQuotation, bool>> predicate) =>
                {
                    // First call is to find entity by ID
                    // Second call is to check duplicate (which should return null)
                    // This is tricky with Moq sequence.
                    // Let's simplify: 
                    // The service calls FirstOrDefaultAsync(pq => pq.PriceQuotationId == id) first.
                    // Then if FeeType changed, calls FirstOrDefaultAsync(pq => ... duplicate check ...).
                    
                    // We can setup specific predicates if we can inspect expression, but that's hard.
                    // Alternatively, we can use a callback or separate setups if possible.
                    // Or just return entity for first call and null for second.
                    return null; 
                });
            
            // Re-setup correctly
            _mockRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PriceQuotation, bool>>>()))
                .ReturnsAsync(entity) // Found entity
                .ReturnsAsync((PriceQuotation?)null); // No duplicate

            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(id, updateDto);

            // Assert
            result.Should().BeTrue();
            _mockRepo.Verify(r => r.UpdateAsync(entity), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_KhiIdTonTai_NenXoa()
        {
            // Arrange
            var id = "pq1";
            var entity = new PriceQuotation { PriceQuotationId = id };
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PriceQuotation, bool>>>()))
                .ReturnsAsync(entity);
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            result.Should().BeTrue();
            _mockRepo.Verify(r => r.RemoveAsync(entity), Times.Once);
        }
    }
}

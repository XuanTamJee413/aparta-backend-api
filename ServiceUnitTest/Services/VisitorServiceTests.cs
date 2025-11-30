using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using AutoMapper;
using FluentAssertions;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Services
{
    public class VisitorServiceTests
    {
        private readonly Mock<IVisitorRepository> _mockVisitorRepo;
        private readonly Mock<IVisitLogRepository> _mockVisitLogRepo;
        private readonly Mock<IRepository<Apartment>> _mockApartmentRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly VisitorService _service;

        public VisitorServiceTests()
        {
            _mockVisitorRepo = new Mock<IVisitorRepository>();
            _mockVisitLogRepo = new Mock<IVisitLogRepository>();
            _mockApartmentRepo = new Mock<IRepository<Apartment>>();
            _mockMapper = new Mock<IMapper>();

            _service = new VisitorService(
                _mockVisitorRepo.Object,
                _mockVisitLogRepo.Object,
                _mockApartmentRepo.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task CreateVisitAsync_KhiThongTinHopLe_NenTaoVisitorVaLog()
        {
            // Arrange
            var dto = new VisitorCreateDto { ApartmentId = "apt1", IdNumber = "123", FullName = "John", Status = "Checked-in" };
            var apartment = new Apartment { ApartmentId = "apt1", Code = "A101" };
            var visitor = new Visitor { VisitorId = "v1", IdNumber = "123", FullName = "John" };
            var log = new VisitLog { VisitLogId = "vl1", VisitorId = "v1", ApartmentId = "apt1" };

            _mockApartmentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _mockVisitorRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Visitor, bool>>>()))
                .ReturnsAsync((Visitor?)null);

            _mockMapper.Setup(m => m.Map<Visitor>(dto)).Returns(visitor);
            _mockMapper.Setup(m => m.Map<VisitLog>(dto)).Returns(log);
            _mockMapper.Setup(m => m.Map<VisitorDto>(visitor)).Returns(new VisitorDto { VisitorId = "v1" });

            // Act
            var result = await _service.CreateVisitAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.VisitorId.Should().Be("v1");
            _mockVisitorRepo.Verify(r => r.AddAsync(visitor), Times.Once);
            _mockVisitLogRepo.Verify(r => r.AddAsync(log), Times.Once);
            _mockVisitorRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateVisitAsync_KhiCanHoKhongTonTai_NenNemValidationException()
        {
            // Arrange
            var dto = new VisitorCreateDto { ApartmentId = "invalid" };
            _mockApartmentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync((Apartment?)null);

            // Act
            Func<Task> act = async () => await _service.CreateVisitAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*không tồn tại*");
        }

        [Fact]
        public async Task CreateVisitAsync_KhiVisitorDaTonTai_NenNemValidationException()
        {
            // Arrange
            var dto = new VisitorCreateDto { ApartmentId = "apt1", IdNumber = "123" };
            var apartment = new Apartment { ApartmentId = "apt1" };
            var existingVisitor = new Visitor { VisitorId = "v1", IdNumber = "123" };

            _mockApartmentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _mockVisitorRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Visitor, bool>>>()))
                .ReturnsAsync(existingVisitor);

            // Act
            Func<Task> act = async () => await _service.CreateVisitAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*đã tồn tại*");
        }

        [Fact]
        public async Task CreateVisitAsync_KhiCheckInTuongLai_NenSetStatusPending()
        {
            // Arrange
            var futureTime = DateTime.Now.AddHours(1).ToString("o");
            var dto = new VisitorCreateDto { ApartmentId = "apt1", IdNumber = "123", Status = "Pending", CheckinTime = futureTime };
            var apartment = new Apartment { ApartmentId = "apt1" };
            var visitor = new Visitor { VisitorId = "v1" };
            var log = new VisitLog { VisitLogId = "vl1" };

            _mockApartmentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _mockVisitorRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Visitor, bool>>>()))
                .ReturnsAsync((Visitor?)null);

            _mockMapper.Setup(m => m.Map<Visitor>(dto)).Returns(visitor);
            _mockMapper.Setup(m => m.Map<VisitLog>(dto)).Returns(log);
            _mockMapper.Setup(m => m.Map<VisitorDto>(visitor)).Returns(new VisitorDto());

            // Act
            await _service.CreateVisitAsync(dto);

            // Assert
            log.Status.Should().Be("Pending");
            log.CheckinTime.Should().NotBe(default);
        }
    }
}

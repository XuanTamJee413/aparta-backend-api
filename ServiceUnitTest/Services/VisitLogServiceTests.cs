using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Profiles;
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
    public class VisitLogServiceTests
    {
        private readonly Mock<IVisitLogRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly VisitLogService _service;

        public VisitLogServiceTests()
        {
            _mockRepo = new Mock<IVisitLogRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = config.CreateMapper();

            _service = new VisitLogService(_mockRepo.Object, _mapper);
        }

        [Fact]
        public async Task GetStaffViewLogsAsync_KhiCoThamSo_NenTraVePagedList()
        {
            // Arrange
            var queryParams = new VisitorQueryParameters { PageNumber = 1, PageSize = 10, SearchTerm = "John", SortColumn = "VisitorFullName", SortDirection = "asc" };
            
            var visitor = new Visitor { VisitorId = "v1", FullName = "John Doe" };
            var apartment = new Apartment { ApartmentId = "apt1", Code = "A101" };
            var logs = new List<VisitLog>
            {
                new VisitLog { VisitLogId = "vl1", VisitorId = "v1", ApartmentId = "apt1", CheckinTime = DateTime.Now, Visitor = visitor, Apartment = apartment }
            }.AsQueryable();

            _mockRepo.Setup(r => r.GetStaffViewLogsQuery()).Returns(new TestAsyncEnumerable<VisitLog>(logs).AsQueryable());

            // Act
            var result = await _service.GetStaffViewLogsAsync(queryParams);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().VisitorFullName.Should().Be("John Doe");
        }

        [Fact]
        public async Task CheckInAsync_KhiStatusPending_NenCapNhatThanhCheckedIn()
        {
            // Arrange
            var logId = "vl1";
            var log = new VisitLog { VisitLogId = logId, Status = "Pending" };
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(log);
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.CheckInAsync(logId);

            // Assert
            result.Should().BeTrue();
            log.Status.Should().Be("Checked-in");
            log.CheckinTime.Should().NotBe(default);
            _mockRepo.Verify(r => r.UpdateAsync(log), Times.Once);
        }

        [Fact]
        public async Task CheckOutAsync_KhiStatusCheckedIn_NenCapNhatThanhCheckedOut()
        {
            // Arrange
            var logId = "vl1";
            var log = new VisitLog { VisitLogId = logId, Status = "Checked-in" };
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(log);
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.CheckOutAsync(logId);

            // Assert
            result.Should().BeTrue();
            log.Status.Should().Be("Checked-out");
            log.CheckoutTime.Should().NotBeNull();
            _mockRepo.Verify(r => r.UpdateAsync(log), Times.Once);
        }

        [Fact]
        public async Task CheckOutAsync_KhiStatusKhongPhaiCheckedIn_NenTraVeFalse()
        {
            // Arrange
            var logId = "vl1";
            var log = new VisitLog { VisitLogId = logId, Status = "Pending" };
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(log);

            // Act
            var result = await _service.CheckOutAsync(logId);

            // Assert
            result.Should().BeFalse();
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
        }
    }
}

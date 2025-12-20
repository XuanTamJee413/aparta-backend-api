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
using System.Collections.Generic;
using System.Linq;
using System;
using System.Linq.Expressions;

namespace ServiceUnitTest.Services
{
    public class VisitLogServiceTests
    {
        private readonly Mock<IVisitLogRepository> _mockRepo;
        private readonly Mock<IRepository<StaffBuildingAssignment>> _mockAssignmentRepo;
        private readonly Mock<IRepository<User>> _mockUserRepo;
        private readonly IMapper _mapper;
        private readonly VisitLogService _service;

        public VisitLogServiceTests()
        {
            _mockRepo = new Mock<IVisitLogRepository>();
            _mockAssignmentRepo = new Mock<IRepository<StaffBuildingAssignment>>();
            _mockUserRepo = new Mock<IRepository<User>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = config.CreateMapper();

            _service = new VisitLogService(
                _mockRepo.Object, 
                _mockAssignmentRepo.Object, 
                _mockUserRepo.Object, 
                _mapper
            );
        }

        [Fact]
        public async Task GetStaffViewLogsAsync_WithValidParams_ReturnsPagedList()
        {
            // Arrange
            var userId = "staff1";
            var queryParams = new VisitorQueryParameters { PageNumber = 1, PageSize = 10, SearchTerm = "John", SortColumn = "VisitorFullName", SortDirection = "asc" };
            
            var visitor = new Visitor { VisitorId = "v1", FullName = "John Doe" };
            var apartment = new Apartment { ApartmentId = "apt1", Code = "A101", BuildingId = "b1" };
            var logs = new List<VisitLog>
            {
                new VisitLog { VisitLogId = "vl1", VisitorId = "v1", ApartmentId = "apt1", CheckinTime = DateTime.Now, Visitor = visitor, Apartment = apartment }
            }.AsQueryable();

            var assignments = new List<StaffBuildingAssignment>
            {
                new StaffBuildingAssignment { UserId = userId, BuildingId = "b1", IsActive = true }
            };

            _mockRepo.Setup(r => r.GetStaffViewLogsQuery()).Returns(new TestAsyncEnumerable<VisitLog>(logs).AsQueryable());
            _mockAssignmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StaffBuildingAssignment, bool>>>()))
                .ReturnsAsync(assignments);

            // Act
            var result = await _service.GetStaffViewLogsAsync(queryParams, userId);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().VisitorFullName.Should().Be("John Doe");
        }

        [Fact]
        public async Task CheckInAsync_WhenStatusPending_UpdatesToCheckedIn()
        {
            // Arrange
            var logId = "vl1";
            var log = new VisitLog { VisitLogId = logId, Status = "Pending" };
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
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
        public async Task CheckOutAsync_WhenStatusCheckedIn_UpdatesToCheckedOut()
        {
            // Arrange
            var logId = "vl1";
            var log = new VisitLog { VisitLogId = logId, Status = "Checked-in" };
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
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
        public async Task CheckOutAsync_WhenStatusNotCheckedIn_ReturnsFalse()
        {
            // Arrange
            var logId = "vl1";
            var log = new VisitLog { VisitLogId = logId, Status = "Pending" };
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(log);

            // Act
            var result = await _service.CheckOutAsync(logId);

            // Assert
            result.Should().BeFalse();
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
        }
    }
}

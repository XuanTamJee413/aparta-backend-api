using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.TestVisitLog
{
    public class VisitLogServiceComprehensiveTests
    {
        private readonly Mock<IVisitLogRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly VisitLogService _service;
        private readonly ApartaDbContext _context;

        public VisitLogServiceComprehensiveTests()
        {
            // Setup InMemory DbContext
            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test class instance
                .Options;
            _context = new ApartaDbContext(options);

            _repoMock = new Mock<IVisitLogRepository>();

            // Setup real AutoMapper configuration
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<VisitLog, VisitLogStaffViewDto>()
                   .ForMember(dest => dest.VisitorFullName, opt => opt.MapFrom(src => src.Visitor.FullName))
                   .ForMember(dest => dest.ApartmentCode, opt => opt.MapFrom(src => src.Apartment.Code))
                   .ForMember(dest => dest.VisitorIdNumber, opt => opt.MapFrom(src => src.Visitor.IdNumber));
            });
            _mapper = config.CreateMapper();

            _service = new VisitLogService(_repoMock.Object, _mapper);
        }

        private VisitLog CreateVisitLog(string id, string visitorName, string apartmentCode, DateTime checkinTime, Building building)
        {
            return new VisitLog
            {
                VisitLogId = id,
                Status = "Pending",
                CheckinTime = checkinTime,
                VisitorId = $"v{id}",
                ApartmentId = $"a{id}",
                Visitor = new Visitor { VisitorId = $"v{id}", FullName = visitorName },
                Apartment = new Apartment 
                { 
                    ApartmentId = $"a{id}", 
                    Code = apartmentCode, 
                    BuildingId = building.BuildingId, 
                    Building = building,
                    Status = "Active"
                }
            };
        }

        private Building CreateBuilding()
        {
            var project = new Project 
            { 
                ProjectId = "p1", 
                ProjectCode = "P1", 
                Name = "Project 1" 
            };
            return new Building 
            { 
                BuildingId = "b1", 
                BuildingCode = "B1", 
                Name = "Building 1",
                ProjectId = "p1",
                Project = project
            };
        }

        #region GetStaffViewLogsAsync Tests

        [Fact]
        public async Task GetStaffViewLogsAsync_ShouldReturnAllLogs_WhenNoFiltersProvided()
        {
            // Arrange
            var building = CreateBuilding();
            var data = new List<VisitLog>
            {
                CreateVisitLog("1", "John Doe", "A101", DateTime.UtcNow.AddHours(-1), building),
                CreateVisitLog("2", "Jane Smith", "B202", DateTime.UtcNow, building)
            };
            await _context.VisitLogs.AddRangeAsync(data);
            await _context.SaveChangesAsync();

            _repoMock.Setup(r => r.GetStaffViewLogsQuery()).Returns(_context.VisitLogs.AsQueryable());

            var queryParams = new VisitorQueryParameters { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetStaffViewLogsAsync(queryParams);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetStaffViewLogsAsync_ShouldFilterByApartmentId()
        {
            // Arrange
            var building = CreateBuilding();
            var log1 = CreateVisitLog("1", "Visitor 1", "A101", DateTime.UtcNow, building);
            log1.ApartmentId = "apt1";
            log1.Apartment.ApartmentId = "apt1";

            var log2 = CreateVisitLog("2", "Visitor 2", "B202", DateTime.UtcNow, building);
            log2.ApartmentId = "apt2";
            log2.Apartment.ApartmentId = "apt2";

            await _context.VisitLogs.AddRangeAsync(log1, log2);
            await _context.SaveChangesAsync();

            _repoMock.Setup(r => r.GetStaffViewLogsQuery()).Returns(_context.VisitLogs.AsQueryable());

            var queryParams = new VisitorQueryParameters { ApartmentId = "apt1", PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetStaffViewLogsAsync(queryParams);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().VisitLogId.Should().Be("1");
        }

        [Fact]
        public async Task GetStaffViewLogsAsync_ShouldFilterBySearchTerm_VisitorName()
        {
            // Arrange
            var building = CreateBuilding();
            var data = new List<VisitLog>
            {
                CreateVisitLog("1", "Alice Wonderland", "A101", DateTime.UtcNow, building),
                CreateVisitLog("2", "Bob Builder", "B202", DateTime.UtcNow, building)
            };
            await _context.VisitLogs.AddRangeAsync(data);
            await _context.SaveChangesAsync();

            _repoMock.Setup(r => r.GetStaffViewLogsQuery()).Returns(_context.VisitLogs.AsQueryable());

            var queryParams = new VisitorQueryParameters { SearchTerm = "Alice", PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetStaffViewLogsAsync(queryParams);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().VisitorFullName.Should().Be("Alice Wonderland");
        }

        [Fact]
        public async Task GetStaffViewLogsAsync_ShouldFilterBySearchTerm_ApartmentCode()
        {
            // Arrange
            var building = CreateBuilding();
            var data = new List<VisitLog>
            {
                CreateVisitLog("1", "Alice", "A101", DateTime.UtcNow, building),
                CreateVisitLog("2", "Bob", "B202", DateTime.UtcNow, building)
            };
            await _context.VisitLogs.AddRangeAsync(data);
            await _context.SaveChangesAsync();

            _repoMock.Setup(r => r.GetStaffViewLogsQuery()).Returns(_context.VisitLogs.AsQueryable());

            var queryParams = new VisitorQueryParameters { SearchTerm = "B202", PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetStaffViewLogsAsync(queryParams);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().ApartmentCode.Should().Be("B202");
        }

        [Fact]
        public async Task GetStaffViewLogsAsync_ShouldSortByVisitorFullName_Ascending()
        {
            // Arrange
            var building = CreateBuilding();
            var data = new List<VisitLog>
            {
                CreateVisitLog("1", "Zack", "A101", DateTime.UtcNow, building),
                CreateVisitLog("2", "Adam", "B202", DateTime.UtcNow, building)
            };
            await _context.VisitLogs.AddRangeAsync(data);
            await _context.SaveChangesAsync();

            _repoMock.Setup(r => r.GetStaffViewLogsQuery()).Returns(_context.VisitLogs.AsQueryable());

            var queryParams = new VisitorQueryParameters { SortColumn = "visitorfullname", SortDirection = "asc", PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetStaffViewLogsAsync(queryParams);

            // Assert
            result.Items.First().VisitorFullName.Should().Be("Adam");
            result.Items.Last().VisitorFullName.Should().Be("Zack");
        }

        [Fact]
        public async Task GetStaffViewLogsAsync_ShouldSortByCheckinTime_Descending_Default()
        {
            // Arrange
            var building = CreateBuilding();
            var now = DateTime.UtcNow;
            var data = new List<VisitLog>
            {
                CreateVisitLog("1", "Visitor 1", "A101", now.AddHours(-2), building),
                CreateVisitLog("2", "Visitor 2", "B202", now, building)
            };
            await _context.VisitLogs.AddRangeAsync(data);
            await _context.SaveChangesAsync();

            _repoMock.Setup(r => r.GetStaffViewLogsQuery()).Returns(_context.VisitLogs.AsQueryable());

            var queryParams = new VisitorQueryParameters { PageNumber = 1, PageSize = 10 }; // Default sort is CheckinTime Desc

            // Act
            var result = await _service.GetStaffViewLogsAsync(queryParams);

            // Assert
            result.Items.First().VisitLogId.Should().Be("2");
            result.Items.Last().VisitLogId.Should().Be("1");
        }

        [Fact]
        public async Task GetStaffViewLogsAsync_ShouldPaginateCorrectly()
        {
            // Arrange
            var building = CreateBuilding();
            var data = Enumerable.Range(1, 20).Select(i => CreateVisitLog(i.ToString(), $"Visitor {i}", $"A{i}", DateTime.UtcNow.AddMinutes(i), building)).ToList();
            await _context.VisitLogs.AddRangeAsync(data);
            await _context.SaveChangesAsync();

            _repoMock.Setup(r => r.GetStaffViewLogsQuery()).Returns(_context.VisitLogs.AsQueryable());

            var queryParams = new VisitorQueryParameters { PageNumber = 2, PageSize = 5, SortColumn = "checkintime", SortDirection = "asc" };

            // Act
            var result = await _service.GetStaffViewLogsAsync(queryParams);

            // Assert
            result.TotalCount.Should().Be(20);
            result.Items.Should().HaveCount(5);
            result.Items.First().VisitLogId.Should().Be("6"); // Page 2 starts at item 6
            result.Items.Last().VisitLogId.Should().Be("10");
        }

        #endregion

        #region CheckInAsync Tests

        [Fact]
        public async Task CheckInAsync_ShouldReturnTrue_WhenStatusIsPending()
        {
            // Arrange
            var visitLog = new VisitLog { VisitLogId = "1", Status = "Pending" };
            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(visitLog);
            _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.CheckInAsync("1");

            // Assert
            result.Should().BeTrue();
            visitLog.Status.Should().Be("Checked-in");
            visitLog.CheckinTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _repoMock.Verify(r => r.UpdateAsync(visitLog), Times.Once);
        }

        [Fact]
        public async Task CheckInAsync_ShouldReturnFalse_WhenLogNotFound()
        {
            // Arrange
            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync((VisitLog)null);

            // Act
            var result = await _service.CheckInAsync("1");

            // Assert
            result.Should().BeFalse();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
        }

        [Fact]
        public async Task CheckInAsync_ShouldReturnFalse_WhenStatusIsNotPending()
        {
            // Arrange
            var visitLog = new VisitLog { VisitLogId = "1", Status = "Checked-in" };
            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(visitLog);

            // Act
            var result = await _service.CheckInAsync("1");

            // Assert
            result.Should().BeFalse();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
        }

        [Fact]
        public async Task CheckInAsync_ShouldReturnFalse_WhenSaveFails()
        {
            // Arrange
            var visitLog = new VisitLog { VisitLogId = "1", Status = "Pending" };
            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(visitLog);
            _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

            // Act
            var result = await _service.CheckInAsync("1");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CheckOutAsync Tests

        [Fact]
        public async Task CheckOutAsync_ShouldReturnTrue_WhenStatusIsCheckedIn()
        {
            // Arrange
            var visitLog = new VisitLog { VisitLogId = "1", Status = "Checked-in" };
            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(visitLog);
            _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.CheckOutAsync("1");

            // Assert
            result.Should().BeTrue();
            visitLog.Status.Should().Be("Checked-out");
            visitLog.CheckoutTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _repoMock.Verify(r => r.UpdateAsync(visitLog), Times.Once);
        }

        [Fact]
        public async Task CheckOutAsync_ShouldReturnFalse_WhenLogNotFound()
        {
            // Arrange
            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync((VisitLog)null);

            // Act
            var result = await _service.CheckOutAsync("1");

            // Assert
            result.Should().BeFalse();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
        }

        [Fact]
        public async Task CheckOutAsync_ShouldReturnFalse_WhenStatusIsNotCheckedIn()
        {
            // Arrange
            var visitLog = new VisitLog { VisitLogId = "1", Status = "Pending" };
            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(visitLog);

            // Act
            var result = await _service.CheckOutAsync("1");

            // Assert
            result.Should().BeFalse();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
        }

        [Fact]
        public async Task CheckOutAsync_ShouldReturnFalse_WhenSaveFails()
        {
            // Arrange
            var visitLog = new VisitLog { VisitLogId = "1", Status = "Checked-in" };
            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(visitLog);
            _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

            // Act
            var result = await _service.CheckOutAsync("1");

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}

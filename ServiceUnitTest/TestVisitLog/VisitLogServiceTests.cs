using ApartaAPI.DTOs.VisitLogs;
using ProjectTask = ApartaAPI.Models.Task;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using AutoMapper;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ServiceUnitTest.TestVisitLog
{
    public class VisitLogServiceTests 
    {
        private readonly Mock<IVisitLogRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly VisitLogService _service;

        public VisitLogServiceTests()
        {
            _repoMock = new Mock<IVisitLogRepository>();
            _mapperMock = new Mock<IMapper>();
            _service = new VisitLogService(_repoMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetStaffViewLogsAsync_WhenLogsExist_ShouldReturnMappedDtos()
        {
            var visitLogsFromRepo = new List<VisitLog>
            {
                new VisitLog { VisitLogId = "log1", Visitor = new Visitor{ FullName = "Visitor 1"}, Apartment = new Apartment { Code = "A101"} },
                new VisitLog { VisitLogId = "log2", Visitor = new Visitor{ FullName = "Visitor 2"}, Apartment = new Apartment { Code = "B202"} }
            };

            var expectedDtos = new List<VisitLogStaffViewDto>
            {
                new VisitLogStaffViewDto { VisitLogId = "log1", VisitorFullName = "Visitor 1", ApartmentCode = "A101" },
                new VisitLogStaffViewDto { VisitLogId = "log2", VisitorFullName = "Visitor 2", ApartmentCode = "B202" }
            };

            _repoMock.Setup(r => r.GetStaffViewLogsAsync()).ReturnsAsync(visitLogsFromRepo);

            _mapperMock.Setup(m => m.Map<IEnumerable<VisitLogStaffViewDto>>(visitLogsFromRepo))
                .Returns(expectedDtos);

            var result = await _service.GetStaffViewLogsAsync();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDtos); 
            result.Should().HaveCount(2);
            _repoMock.Verify(r => r.GetStaffViewLogsAsync(), Times.Once);
            _mapperMock.Verify(m => m.Map<IEnumerable<VisitLogStaffViewDto>>(visitLogsFromRepo), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetStaffViewLogsAsync_WhenNoLogsExist_ShouldReturnEmptyList()
        {
            var emptyLogsFromRepo = new List<VisitLog>();

            var expectedEmptyDtos = new List<VisitLogStaffViewDto>();

            _repoMock.Setup(r => r.GetStaffViewLogsAsync()).ReturnsAsync(emptyLogsFromRepo);

            _mapperMock.Setup(m => m.Map<IEnumerable<VisitLogStaffViewDto>>(emptyLogsFromRepo))
                .Returns(expectedEmptyDtos);

            var result = await _service.GetStaffViewLogsAsync();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _repoMock.Verify(r => r.GetStaffViewLogsAsync(), Times.Once);
            _mapperMock.Verify(m => m.Map<IEnumerable<VisitLogStaffViewDto>>(emptyLogsFromRepo), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetStaffViewLogsAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            var expectedException = new InvalidOperationException("Database connection error");
            _repoMock.Setup(r => r.GetStaffViewLogsAsync()).ThrowsAsync(expectedException);

            Func<System.Threading.Tasks.Task> act = async () => await _service.GetStaffViewLogsAsync();

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Database connection error");

            _mapperMock.Verify(m => m.Map<IEnumerable<VisitLogStaffViewDto>>(It.IsAny<IEnumerable<VisitLog>>()), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetStaffViewLogsAsync_WhenMapperThrowsException_ShouldThrowException()
        {
            var visitLogsFromRepo = new List<VisitLog> { new VisitLog { VisitLogId = "log1" } };
            _repoMock.Setup(r => r.GetStaffViewLogsAsync()).ReturnsAsync(visitLogsFromRepo);

            var expectedException = new AutoMapperMappingException("Mapping configuration error");
            _mapperMock.Setup(m => m.Map<IEnumerable<VisitLogStaffViewDto>>(visitLogsFromRepo))
                .Throws(expectedException);

            Func<System.Threading.Tasks.Task> act = async () => await _service.GetStaffViewLogsAsync();

            await act.Should().ThrowAsync<AutoMapperMappingException>()
                     .WithMessage("Mapping configuration error");

            _repoMock.Verify(r => r.GetStaffViewLogsAsync(), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetStaffViewLogsAsync_WhenNavigationPropertiesAreNull_ShouldMapGracefully()
        {
            var visitLogsWithNulls = new List<VisitLog>
            {
                new VisitLog { VisitLogId = "log1", Visitor = null, Apartment = new Apartment { Code = "A101"} }, 
                new VisitLog { VisitLogId = "log2", Visitor = new Visitor{ FullName = "Visitor 2"}, Apartment = null },
                new VisitLog { VisitLogId = "log3", Visitor = null, Apartment = null } // Thiếu cả hai
            };

            var expectedDtosWithNulls = new List<VisitLogStaffViewDto>
            {
                new VisitLogStaffViewDto { VisitLogId = "log1", VisitorFullName = null, VisitorIdNumber = null, ApartmentCode = "A101" },
                new VisitLogStaffViewDto { VisitLogId = "log2", VisitorFullName = "Visitor 2", VisitorIdNumber = null, ApartmentCode = null },
                new VisitLogStaffViewDto { VisitLogId = "log3", VisitorFullName = null, VisitorIdNumber = null, ApartmentCode = null }
            };

            _repoMock.Setup(r => r.GetStaffViewLogsAsync()).ReturnsAsync(visitLogsWithNulls);

            _mapperMock.Setup(m => m.Map<IEnumerable<VisitLogStaffViewDto>>(visitLogsWithNulls))
                .Returns(expectedDtosWithNulls);

            var result = await _service.GetStaffViewLogsAsync();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDtosWithNulls);
            _repoMock.Verify(r => r.GetStaffViewLogsAsync(), Times.Once);
            _mapperMock.Verify(m => m.Map<IEnumerable<VisitLogStaffViewDto>>(visitLogsWithNulls), Times.Once);
        }
    }
}

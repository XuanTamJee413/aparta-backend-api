using Xunit;
using Moq;
using FluentAssertions;
using ApartaAPI.Services;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Models;
using ApartaAPI.DTOs.VisitLogs;
using AutoMapper;
using System.Threading.Tasks;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ServiceUnitTest.TestVisitLog
{
    public class TestCheckIn
    {
        private readonly Mock<IVisitLogRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly VisitLogService _service;

        public TestCheckIn()
        {
            _repoMock = new Mock<IVisitLogRepository>();
            _mapperMock = new Mock<IMapper>();
            _service = new VisitLogService(_repoMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task CheckInAsync_WhenLogIsPendingAndSaveSucceeds_ShouldReturnTrueAndSetStatusCheckedIn()
        {
            var visitLogId = "pending-log-id";
            var pendingLog = new VisitLog
            {
                VisitLogId = visitLogId,
                Status = "Pending"
            };

            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(pendingLog);

            _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

            var result = await _service.CheckInAsync(visitLogId);

            result.Should().BeTrue();
            pendingLog.Status.Should().Be("Checked-in");
            pendingLog.CheckinTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _repoMock.Verify(r => r.UpdateAsync(pendingLog), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task CheckInAsync_WhenLogIsNotFound_ShouldReturnFalse()
        {
            var nonExistentId = "non-existent-id";

            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync((VisitLog)null);

            var result = await _service.CheckInAsync(nonExistentId);

            result.Should().BeFalse();

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task CheckInAsync_WhenLogStatusIsNotPending_ShouldReturnFalse()
        {
            var visitLogId = "checked-in-log-id";
            var alreadyCheckedInLog = new VisitLog
            {
                VisitLogId = visitLogId,
                Status = "Checked-in"
            };

            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(alreadyCheckedInLog);

            var result = await _service.CheckInAsync(visitLogId);

            result.Should().BeFalse();

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task CheckInAsync_WhenLogIsPendingButSaveFails_ShouldReturnFalse()
        {
            var visitLogId = "pending-log-save-fail-id";
            var pendingLog = new VisitLog
            {
                VisitLogId = visitLogId,
                Status = "Pending"
            };

            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(pendingLog);

            _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

            var result = await _service.CheckInAsync(visitLogId);

            result.Should().BeFalse();

            pendingLog.Status.Should().Be("Checked-in");
            pendingLog.CheckinTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _repoMock.Verify(r => r.UpdateAsync(pendingLog), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
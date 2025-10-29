using Xunit;
using Moq;
using FluentAssertions;
using ApartaAPI.Services;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Models;
using AutoMapper;
using System.Threading.Tasks;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ServiceUnitTest.TestVisitLog
{
    public class TestCheckOut
    {
        private readonly Mock<IVisitLogRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly VisitLogService _service;

        public TestCheckOut()
        {
            _repoMock = new Mock<IVisitLogRepository>();
            _mapperMock = new Mock<IMapper>();
            _service = new VisitLogService(_repoMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task CheckOutAsync_WhenLogIsCheckedInAndSaveSucceeds_ShouldReturnTrueAndSetStatusCheckedOut()
        {
            var visitLogId = "checked-in-log-id";
            var checkedInLog = new VisitLog
            {
                VisitLogId = visitLogId,
                Status = "Checked-in"
            };

            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(checkedInLog);
            _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

            var result = await _service.CheckOutAsync(visitLogId);

            result.Should().BeTrue();
            checkedInLog.Status.Should().Be("Checked-out");
            checkedInLog.CheckoutTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _repoMock.Verify(r => r.UpdateAsync(checkedInLog), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task CheckOutAsync_WhenLogIsNotFound_ShouldReturnFalse()
        {
            var nonExistentId = "non-existent-id-checkout";

            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync((VisitLog)null);

            var result = await _service.CheckOutAsync(nonExistentId);

            result.Should().BeFalse();

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task CheckOutAsync_WhenLogStatusIsNotCheckedIn_ShouldReturnFalse()
        {
            var visitLogId = "pending-log-id-checkout";
            var pendingLog = new VisitLog
            {
                VisitLogId = visitLogId,
                Status = "Pending"
            };

            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(pendingLog);

            var result = await _service.CheckOutAsync(visitLogId);

            result.Should().BeFalse();

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VisitLog>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task CheckOutAsync_WhenLogIsCheckedInButSaveFails_ShouldReturnFalse()
        {
            var visitLogId = "checked-in-log-save-fail-id";
            var checkedInLog = new VisitLog
            {
                VisitLogId = visitLogId,
                Status = "Checked-in"
            };

            _repoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<VisitLog, bool>>>()))
                .ReturnsAsync(checkedInLog);
            _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

            var result = await _service.CheckOutAsync(visitLogId);

            result.Should().BeFalse();

            checkedInLog.Status.Should().Be("Checked-out");
            checkedInLog.CheckoutTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _repoMock.Verify(r => r.UpdateAsync(checkedInLog), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
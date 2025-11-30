using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Repositories
{
    public class VisitorRepositoryTests
    {
        private readonly ApartaDbContext _context;
        private readonly VisitorRepository _repository;

        public VisitorRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);
            _repository = new VisitorRepository(_context);
        }

        [Fact]
        public async Task AddAsync_KhiVisitorHopLe_NenThemThanhCong()
        {
            // Arrange
            var visitor = new Visitor { VisitorId = "v1", FullName = "John Doe", IdNumber = "123456789", Phone = "1234567890" };

            // Act
            await _repository.AddAsync(visitor);

            // Assert
            var addedVisitor = await _context.Visitors.FindAsync("v1");
            addedVisitor.Should().NotBeNull();
            addedVisitor!.FullName.Should().Be("John Doe");
        }

        [Fact]
        public async Task GetByIdAsync_KhiIdTonTai_NenTraVeVisitor()
        {
            // Arrange
            var visitor = new Visitor { VisitorId = "v1", FullName = "John Doe", IdNumber = "123456789", Phone = "1234567890" };
            await _context.Visitors.AddAsync(visitor);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync("v1");

            // Assert
            result.Should().NotBeNull();
            result!.VisitorId.Should().Be("v1");
        }

        [Fact]
        public async Task UpdateAsync_KhiVisitorHopLe_NenCapNhatThanhCong()
        {
            // Arrange
            var visitor = new Visitor { VisitorId = "v1", FullName = "John Doe", IdNumber = "123456789", Phone = "1234567890" };
            await _context.Visitors.AddAsync(visitor);
            await _context.SaveChangesAsync();

            // Act
            visitor.FullName = "Jane Doe";
            await _repository.UpdateAsync(visitor);

            // Assert
            var updatedVisitor = await _context.Visitors.FindAsync("v1");
            updatedVisitor.Should().NotBeNull();
            updatedVisitor!.FullName.Should().Be("Jane Doe");
        }
    }
}

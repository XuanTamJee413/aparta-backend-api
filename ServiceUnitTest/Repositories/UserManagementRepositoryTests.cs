using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;
using ApartaAPI.Models;
using ApartaAPI.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Repositories
{
    public class UserManagementRepositoryTests
    {
        private readonly ApartaDbContext _context;
        private readonly UserManagementRepository _repository;

        public UserManagementRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);
            _repository = new UserManagementRepository(_context);
        }

        [Fact]
        public async Task GetPagedUsersAsync_KhiCoThamSo_NenTraVePagedList()
        {
            // Arrange
            var role = new Role { RoleId = "r1", RoleName = "Staff" };
            var u1 = new User { UserId = "u1", Name = "User 1", Role = role, Status = "Active", PasswordHash = "hash", Email = "u1@test.com" };
            var u2 = new User { UserId = "u2", Name = "User 2", Role = role, Status = "Active", PasswordHash = "hash", Email = "u2@test.com" };
            
            await _context.Roles.AddAsync(role);
            await _context.Users.AddRangeAsync(u1, u2);
            await _context.SaveChangesAsync();

            var queryParams = new UserQueryParams { PageNumber = 1, PageSize = 10 };
            var rolesToInclude = new List<string> { "Staff" };

            // Act
            var result = await _repository.GetPagedUsersAsync(queryParams, rolesToInclude);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetStaffAssignmentsAsync_KhiUserIdTonTai_NenTraVeDanhSachAssignment()
        {
            // Arrange
            var userId = "u1";
            var building = new Building { BuildingId = "b1", BuildingCode = "B1", Name = "Building 1", ProjectId = "proj1" };
            var assignment = new StaffBuildingAssignment 
            { 
                AssignmentId = "a1", 
                UserId = userId, 
                BuildingId = "b1", 
                Building = building,
                AssignmentStartDate = DateOnly.FromDateTime(DateTime.Now),
                IsActive = true
            };

            await _context.Buildings.AddAsync(building);
            await _context.StaffBuildingAssignments.AddAsync(assignment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetStaffAssignmentsAsync(userId);

            // Assert
            result.Should().HaveCount(1);
            result.First().Building.Should().NotBeNull();
            result.First().Building.BuildingCode.Should().Be("B1");
        }
    }
}

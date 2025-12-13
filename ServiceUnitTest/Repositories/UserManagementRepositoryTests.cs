using ApartaAPI.Data;
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
        public async Task GetUsersQuery_WithSpecificRoles_ReturnsMatchingUsers()
        {
            // Arrange
            var roleStaff = new Role { RoleId = "r1", RoleName = "Staff" };
            var roleResident = new Role { RoleId = "r2", RoleName = "Resident" };
            
            var u1 = new User { UserId = "u1", Name = "Staff 1", Role = roleStaff, IsDeleted = false };
            var u2 = new User { UserId = "u2", Name = "Resident 1", Role = roleResident, IsDeleted = false };
            
            await _context.Roles.AddRangeAsync(roleStaff, roleResident);
            await _context.Users.AddRangeAsync(u1, u2);
            await _context.SaveChangesAsync();

            var rolesToInclude = new List<string> { "Staff" };

            // Act
            var query = _repository.GetUsersQuery(rolesToInclude);
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Staff 1");
        }

        [Fact]
        public async Task GetUsersQuery_WithNoRoles_ReturnsAllActiveUsers()
        {
            // Arrange
            var role = new Role { RoleId = "r1", RoleName = "Staff" };
            var u1 = new User { UserId = "u1", Name = "User 1", Role = role, IsDeleted = false };
            var u2 = new User { UserId = "u2", Name = "User 2", Role = role, IsDeleted = false };
            
            await _context.Roles.AddAsync(role);
            await _context.Users.AddRangeAsync(u1, u2);
            await _context.SaveChangesAsync();

            // Act
            var query = _repository.GetUsersQuery(new List<string>()); // Empty list
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetUsersQuery_WithNullRoles_ReturnsAllActiveUsers()
        {
            // Arrange
            var role = new Role { RoleId = "r1", RoleName = "Staff" };
            var u1 = new User { UserId = "u1", Name = "User 1", Role = role, IsDeleted = false };
            
            await _context.Roles.AddAsync(role);
            await _context.Users.AddAsync(u1);
            await _context.SaveChangesAsync();

            // Act
            var query = _repository.GetUsersQuery(null);
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetUsersQuery_ShouldExcludeDeletedUsers()
        {
            // Arrange
            var role = new Role { RoleId = "r1", RoleName = "Staff" };
            var u1 = new User { UserId = "u1", Name = "Active User", Role = role, IsDeleted = false };
            var u2 = new User { UserId = "u2", Name = "Deleted User", Role = role, IsDeleted = true };
            
            await _context.Roles.AddAsync(role);
            await _context.Users.AddRangeAsync(u1, u2);
            await _context.SaveChangesAsync();

            // Act
            var query = _repository.GetUsersQuery(null);
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().UserId.Should().Be("u1");
        }

        [Fact]
        public async Task GetUsersQuery_WithNonExistentRole_ReturnsEmpty()
        {
            // Arrange
            var role = new Role { RoleId = "r1", RoleName = "Staff" };
            var u1 = new User { UserId = "u1", Name = "User 1", Role = role, IsDeleted = false };
            
            await _context.Roles.AddAsync(role);
            await _context.Users.AddAsync(u1);
            await _context.SaveChangesAsync();

            var rolesToInclude = new List<string> { "Admin" };

            // Act
            var query = _repository.GetUsersQuery(rolesToInclude);
            var result = await query.ToListAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetStaffAssignmentsQuery_WithValidId_ReturnsActiveAssignments()
        {
            // Arrange
            var userId = "u1";
            var building = new Building { BuildingId = "b1", BuildingCode = "B1" };
            var activeAssignment = new StaffBuildingAssignment 
            { 
                AssignmentId = "a1", UserId = userId, BuildingId = "b1", Building = building, IsActive = true 
            };
            var inactiveAssignment = new StaffBuildingAssignment 
            { 
                AssignmentId = "a2", UserId = userId, BuildingId = "b1", Building = building, IsActive = false 
            };

            await _context.Buildings.AddAsync(building);
            await _context.StaffBuildingAssignments.AddRangeAsync(activeAssignment, inactiveAssignment);
            await _context.SaveChangesAsync();

            // Act
            var query = _repository.GetStaffAssignmentsQuery(userId);
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().AssignmentId.Should().Be("a1");
            result.First().Building.Should().NotBeNull();
        }

        [Fact]
        public async Task GetStaffAssignmentsQuery_WithInvalidId_ReturnsEmpty()
        {
            // Arrange
            var userId = "u1";
            var assignment = new StaffBuildingAssignment { AssignmentId = "a1", UserId = userId, IsActive = true };
            await _context.StaffBuildingAssignments.AddAsync(assignment);
            await _context.SaveChangesAsync();

            // Act
            var query = _repository.GetStaffAssignmentsQuery("non-existent-id");
            var result = await query.ToListAsync();

            // Assert
            result.Should().BeEmpty();
        }
    }
}

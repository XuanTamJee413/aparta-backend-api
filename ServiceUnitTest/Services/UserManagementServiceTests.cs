using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Services
{
    public class UserManagementServiceTests
    {
        private readonly Mock<IUserManagementRepository> _mockUserManagementRepo;
        private readonly Mock<IRepository<User>> _mockUserRepo;
        private readonly Mock<IRepository<StaffBuildingAssignment>> _mockSbaRepo;
        private readonly Mock<IRepository<Role>> _mockRoleRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ApartaDbContext _context;
        private readonly UserManagementService _service;

        public UserManagementServiceTests()
        {
            _mockUserManagementRepo = new Mock<IUserManagementRepository>();
            _mockUserRepo = new Mock<IRepository<User>>();
            _mockSbaRepo = new Mock<IRepository<StaffBuildingAssignment>>();
            _mockRoleRepo = new Mock<IRepository<Role>>();
            _mockMapper = new Mock<IMapper>();

            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);

            _service = new UserManagementService(
                _mockUserManagementRepo.Object,
                _mockUserRepo.Object,
                _mockSbaRepo.Object,
                _mockRoleRepo.Object,
                _mockMapper.Object,
                _context
            );
        }

        [Fact]
        public async Task GetStaffAccountsAsync_KhiGoi_NenTraVeDanhSachStaff()
        {
            // Arrange
            var queryParams = new UserQueryParams { PageNumber = 1, PageSize = 10 };
            var users = new List<User> { new User { UserId = "u1", Role = new Role { RoleName = "Staff" }, Status = "Active" } };
            var pagedList = new PagedList<User>(users, 1, 1, 10);

            _mockUserManagementRepo.Setup(r => r.GetPagedUsersAsync(queryParams, It.IsAny<List<string>>()))
                .ReturnsAsync(pagedList);
            _mockUserManagementRepo.Setup(r => r.GetStaffAssignmentsAsync("u1")).ReturnsAsync(new List<StaffBuildingAssignment>());

            // Act
            var result = await _service.GetStaffAccountsAsync(queryParams);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().UserId.Should().Be("u1");
        }

        [Fact]
        public async Task GetResidentAccountsAsync_KhiGoi_NenTraVeDanhSachResident()
        {
            // Arrange
            var queryParams = new UserQueryParams { PageNumber = 1, PageSize = 10 };
            var users = new List<User> { new User { UserId = "u1", Role = new Role { RoleName = "Resident" }, Status = "Active" } };
            var pagedList = new PagedList<User>(users, 1, 1, 10);

            _mockUserManagementRepo.Setup(r => r.GetPagedUsersAsync(queryParams, It.IsAny<List<string>>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _service.GetResidentAccountsAsync(queryParams);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateStaffAccountAsync_KhiHopLe_NenTaoStaff()
        {
            // Arrange
            var createDto = new StaffCreateDto { RoleId = "r1", Email = "test@test.com", Phone = "123", Password = "pass" };
            var role = new Role { RoleId = "r1", RoleName = "Staff" };
            var user = new User { UserId = "u1", Role = role, Phone = "123", Status = "active" };

            _mockRoleRepo.Setup(r => r.GetByIdAsync("r1")).ReturnsAsync(role);
            _mockUserRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null); // No duplicate email/phone
            _mockMapper.Setup(m => m.Map<User>(createDto)).Returns(user);
            _mockUserRepo.Setup(r => r.AddAsync(user)).ReturnsAsync(user);

            // Setup for internal GetAccountsByRoleInternalAsync call
            var pagedList = new PagedList<User>(new List<User> { user }, 1, 1, 1);
            _mockUserManagementRepo.Setup(r => r.GetPagedUsersAsync(It.IsAny<UserQueryParams>(), It.IsAny<List<string>>()))
                .ReturnsAsync(pagedList);
            _mockUserManagementRepo.Setup(r => r.GetStaffAssignmentsAsync("u1")).ReturnsAsync(new List<StaffBuildingAssignment>());

            // Act
            var result = await _service.CreateStaffAccountAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("u1");
            _mockUserRepo.Verify(r => r.AddAsync(user), Times.Once);
        }

        [Fact]
        public async Task CreateStaffAccountAsync_KhiEmailTrung_NenNemInvalidOperationException()
        {
            // Arrange
            var createDto = new StaffCreateDto { RoleId = "r1", Email = "test@test.com" };
            var role = new Role { RoleId = "r1" };
            var existingUser = new User { UserId = "u2", Email = "test@test.com" };

            _mockRoleRepo.Setup(r => r.GetByIdAsync("r1")).ReturnsAsync(role);
            _mockUserRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(existingUser); // Duplicate email

            // Act
            Func<Task> act = async () => await _service.CreateStaffAccountAsync(createDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Email đã tồn tại*");
        }

        [Fact]
        public async Task ToggleUserStatusAsync_KhiIdTonTai_NenCapNhatStatus()
        {
            // Arrange
            var userId = "u1";
            var dto = new StatusUpdateDto { Status = "Inactive" };
            var user = new User { UserId = userId, Name = "User", PasswordHash = "hash", Status = "active", Role = new Role { RoleId = "r1", RoleName = "Staff" } };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            
            // For internal check
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Setup for internal GetAccountsByRoleInternalAsync call
            var pagedList = new PagedList<User>(new List<User> { user }, 1, 1, 1);
            _mockUserManagementRepo.Setup(r => r.GetPagedUsersAsync(It.IsAny<UserQueryParams>(), It.IsAny<List<string>>()))
                .ReturnsAsync(pagedList);
            _mockUserManagementRepo.Setup(r => r.GetStaffAssignmentsAsync(userId)).ReturnsAsync(new List<StaffBuildingAssignment>());

            // Act
            var result = await _service.ToggleUserStatusAsync(userId, dto);

            // Assert
            result.Should().NotBeNull();
            user.Status.Should().Be("inactive");
            _mockUserRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateStaffAssignmentAsync_KhiStaffTonTai_NenCapNhatAssignment()
        {
            // Arrange
            var staffId = "u1";
            var updateDto = new AssignmentUpdateDto { BuildingIds = new List<string> { "b1" }, ScopeOfWork = "Work" };
            var staff = new User { UserId = staffId };
            var building = new Building { BuildingId = "b1", BuildingCode = "B01", Name = "Building 1", ProjectId = "p1" };

            _mockUserRepo.Setup(r => r.GetByIdAsync(staffId)).ReturnsAsync(staff);
            _mockSbaRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StaffBuildingAssignment, bool>>>()))
                .ReturnsAsync(new List<StaffBuildingAssignment>()); // No old assignments
            
            await _context.Buildings.AddAsync(building);
            await _context.SaveChangesAsync();

            // Act
            await _service.UpdateStaffAssignmentAsync(staffId, updateDto);

            // Assert
            _mockSbaRepo.Verify(r => r.AddAsync(It.IsAny<StaffBuildingAssignment>()), Times.Once);
            _mockSbaRepo.Verify(r => r.SaveChangesAsync(), Times.Exactly(2)); // Remove old + Add new
        }
    }
}

using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;
using ApartaAPI.Models;
using ApartaAPI.Profiles;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IMapper _mapper;
        private readonly ApartaDbContext _context;
        private readonly Mock<IMailService> _mockMailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly UserManagementService _service;

        public UserManagementServiceTests()
        {
            _mockUserManagementRepo = new Mock<IUserManagementRepository>();
            _mockUserRepo = new Mock<IRepository<User>>();
            _mockSbaRepo = new Mock<IRepository<StaffBuildingAssignment>>();
            _mockRoleRepo = new Mock<IRepository<Role>>();
            _mockMailService = new Mock<IMailService>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Real Mapper Setup
            var config = new MapperConfiguration(cfg => {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = config.CreateMapper();

            // InMemory DbContext Setup
            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new ApartaDbContext(options);

            _service = new UserManagementService(
                _mockUserManagementRepo.Object,
                _mockUserRepo.Object,
                _mockSbaRepo.Object,
                _mockRoleRepo.Object,
                _mapper,
                _context,
                _mockMailService.Object,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task GetStaffAccountsAsync_WithValidParams_ReturnsStaffList()
        {
            // Arrange
            var role = new Role { RoleId = "r1", RoleName = "Staff" };
            var staff = new User { UserId = "u1", Name = "Staff 1", Role = role, Status = "active", CreatedAt = DateTime.UtcNow, PasswordHash = "hash" };
            
            await _context.Roles.AddAsync(role);
            await _context.Users.AddAsync(staff);
            await _context.SaveChangesAsync();

            _mockUserManagementRepo.Setup(r => r.GetUsersQuery(It.IsAny<List<string>>()))
                .Returns(_context.Users.AsQueryable());

            var queryParams = new UserQueryParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetStaffAccountsAsync(queryParams);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().Name.Should().Be("Staff 1");
        }

        [Fact]
        public async Task GetStaffAccountsAsync_WithEmptyResult_ReturnsEmptyList()
        {
            // Arrange
            _mockUserManagementRepo.Setup(r => r.GetUsersQuery(It.IsAny<List<string>>()))
                .Returns(_context.Users.AsQueryable()); // Empty context

            var queryParams = new UserQueryParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetStaffAccountsAsync(queryParams);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetResidentAccountsAsync_WithValidParams_ReturnsResidentList()
        {
            // Arrange
            var role = new Role { RoleId = "r2", RoleName = "Resident" };
            var resident = new User { UserId = "u2", Name = "Resident 1", Role = role, Status = "active", CreatedAt = DateTime.UtcNow, PasswordHash = "hash" };
            
            await _context.Roles.AddAsync(role);
            await _context.Users.AddAsync(resident);
            await _context.SaveChangesAsync();

            _mockUserManagementRepo.Setup(r => r.GetUsersQuery(It.IsAny<List<string>>()))
                .Returns(_context.Users.AsQueryable());

            var queryParams = new UserQueryParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetResidentAccountsAsync(queryParams);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateStaffAccountAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var roleId = "r1";
            var createDto = new StaffCreateDto { RoleId = roleId, Email = "new@staff.com", Phone = "123456789", Password = "password" };
            var role = new Role { RoleId = roleId, RoleName = "Staff" };

            _mockRoleRepo.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(role);
            _mockUserManagementRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null); // No duplicates

            // Act
            var result = await _service.CreateStaffAccountAsync(createDto);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Email.Should().Be("new@staff.com");
            _mockUserManagementRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task CreateStaffAccountAsync_WithInvalidRole_ReturnsFail()
        {
            // Arrange
            var createDto = new StaffCreateDto { RoleId = "invalid_id" };
            _mockRoleRepo.Setup(r => r.GetByIdAsync("invalid_id")).ReturnsAsync((Role?)null);

            // Act
            var result = await _service.CreateStaffAccountAsync(createDto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Role không tồn tại");
        }

        [Fact]
        public async Task CreateStaffAccountAsync_WithDuplicateEmail_ReturnsFail()
        {
            // Arrange
            var createDto = new StaffCreateDto { RoleId = "r1", Email = "exist@staff.com" };
            var role = new Role { RoleId = "r1" };
            var existingUser = new User { UserId = "u_exist", Name = "Existing User", Email = "exist@staff.com", PasswordHash = "hash" };

            _mockRoleRepo.Setup(r => r.GetByIdAsync("r1")).ReturnsAsync(role);
            
            // Setup FirstOrDefaultAsync to return user when checking email
            _mockUserManagementRepo.Setup(r => r.FirstOrDefaultAsync(It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(
                expr => expr.Compile().Invoke(existingUser))))
                .ReturnsAsync(existingUser);
                
            // Simpler approach for FirstOrDefaultAsync mock since expression matching is hard
             _mockUserManagementRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _service.CreateStaffAccountAsync(createDto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("đã tồn tại"); // SM16 DUPLICATE_CODE
        }

        [Fact]

        public async Task ToggleUserStatusAsync_WithValidId_UpdatesStatus()
        {
            // Arrange
            var userId = "u1";
            var dto = new StatusUpdateDto { Status = "Inactive" };
            var user = new User { UserId = userId, Name = "User", Status = "active", Role = new Role { RoleId = "r1", RoleName = "Staff" }, PasswordHash = "hash" };

            _mockUserManagementRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            
            // Need user in context for re-fetching in service
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockUserManagementRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
                            .Callback<User>(u => 
                {
                    _context.Users.Update(u);
                    _context.SaveChanges();
                })
                .ReturnsAsync(user);


            // Act
            var result = await _service.ToggleUserStatusAsync(userId, dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Status.Should().Be("inactive");
            _mockUserManagementRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task ToggleUserStatusAsync_WithInvalidStatus_ReturnsFail()
        {
            // Arrange
            var userId = "u1";
            var dto = new StatusUpdateDto { Status = "InvalidStatus" };
            var user = new User { UserId = userId, Name = "User" };

            _mockUserManagementRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.ToggleUserStatusAsync(userId, dto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Trạng thái chỉ chấp nhận");
        }

        [Fact]
        public async Task ToggleUserStatusAsync_WithNonExistentUser_ReturnsFail()
        {
            // Arrange
            _mockUserManagementRepo.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync((User?)null);

            // Act
            var result = await _service.ToggleUserStatusAsync("u1", new StatusUpdateDto { Status = "Active" });

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy"); // SM01 NO_RESULTS
        }

        [Fact]
        public async Task UpdateStaffAssignmentAsync_WithValidData_UpdatesAssignments()
        {
            // Arrange
            var staffId = "u1";
            var buildingId = "b1";
            var updateDto = new AssignmentUpdateDto { BuildingIds = new List<string> { buildingId }, ScopeOfWork = "Work" };
            var staff = new User { UserId = staffId, Name = "Staff" };
            var building = new Building { BuildingId = buildingId, BuildingCode = "B1", Name = "Building 1", ProjectId = "p1" };

            _mockUserManagementRepo.Setup(r => r.GetByIdAsync(staffId)).ReturnsAsync(staff);
            _mockSbaRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StaffBuildingAssignment, bool>>>()))
                .ReturnsAsync(new List<StaffBuildingAssignment>()); // No old assignments
            
            await _context.Buildings.AddAsync(building);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateStaffAssignmentAsync(staffId, updateDto);

            // Assert
            result.Succeeded.Should().BeTrue();
            _mockSbaRepo.Verify(r => r.AddAsync(It.IsAny<StaffBuildingAssignment>()), Times.Once);
            _mockSbaRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStaffAssignmentAsync_WithInvalidBuilding_ReturnsFail()
        {
            // Arrange
            var staffId = "u1";
            var updateDto = new AssignmentUpdateDto { BuildingIds = new List<string> { "invalid_b" } };
            var staff = new User { UserId = staffId, Name = "Staff" };

            _mockUserManagementRepo.Setup(r => r.GetByIdAsync(staffId)).ReturnsAsync(staff);
            _mockSbaRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StaffBuildingAssignment, bool>>>()))
                .ReturnsAsync(new List<StaffBuildingAssignment>());

            // Act
            var result = await _service.UpdateStaffAssignmentAsync(staffId, updateDto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("không tồn tại");
        }

        [Fact]
        public async Task ResetStaffPasswordAsync_WithValidUser_ResetsPasswordAndSendsEmail()
        {
            // Arrange
            var userId = "u1";
            var user = new User { UserId = userId, Email = "test@test.com", Name = "Test User", PasswordHash = "hash" };
            
            _mockUserManagementRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.ResetStaffPasswordAsync(userId);

            // Assert
            result.Succeeded.Should().BeTrue();
            user.PasswordHash.Should().NotBeNull();
            _mockUserManagementRepo.Verify(r => r.UpdateAsync(user), Times.Once);
            _mockMailService.Verify(m => m.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ResetStaffPasswordAsync_WithNonExistentUser_ReturnsFail()
        {
             // Arrange
            _mockUserManagementRepo.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync((User?)null);

            // Act
            var result = await _service.ResetStaffPasswordAsync("u1");

            // Assert
            result.Succeeded.Should().BeFalse();
        }
    }
}

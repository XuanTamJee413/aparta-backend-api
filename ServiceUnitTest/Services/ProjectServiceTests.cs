using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceUnitTest.Services
{
    public class ProjectServiceTests : IDisposable
    {
        private readonly Mock<IRepository<Project>> _mockRepo;
        private readonly Mock<IRepository<Subscription>> _mockSubRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ApartaDbContext _context;
        private readonly ProjectService _service;

        public ProjectServiceTests()
        {
            _mockRepo = new Mock<IRepository<Project>>();
            _mockSubRepo = new Mock<IRepository<Subscription>>();
            _mockMapper = new Mock<IMapper>();

            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);

            _service = new ProjectService(
                _mockRepo.Object,
                _mockSubRepo.Object,
                _mockMapper.Object,
                _context
            );

            // Setup Repository to sync with InMemory Context
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<Project>()))
                .Callback<Project>(p => _context.Projects.Add(p));
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Project>()))
                .Callback<Project>(p => {
                    if (!_context.Projects.Local.Contains(p)) {
                        _context.Projects.Update(p);
                    }
                });
            _mockRepo.Setup(r => r.SaveChangesAsync())
                .Callback(() => _context.SaveChanges())
                .Returns(System.Threading.Tasks.Task.FromResult(true));
            _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>()))
                .Returns((System.Linq.Expressions.Expression<Func<Project, bool>> predicate) => 
                {
                    return System.Threading.Tasks.Task.FromResult(_context.Projects.FirstOrDefault(predicate.Compile()));
                });
            
            SeedDatabase();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SeedDatabase()
        {
            var projects = new List<Project>
            {
                new Project 
                { 
                    ProjectId = "proj1", 
                    ProjectCode = "CCSR2025", 
                    Name = "Chung Cư Sunrise", 
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    Buildings = new List<Building>
                    {
                        new Building 
                        { 
                            BuildingId = "b1", 
                            BuildingCode = "B1",
                            Name = "Building 1",
                            IsActive = true,
                            Apartments = new List<Apartment>
                            {
                                new Apartment { ApartmentId = "a1", Code = "A101", Status = "Đã Thuê" },
                                new Apartment { ApartmentId = "a2", Code = "A102", Status = "Trống" }
                            }
                        }
                    }
                },
                new Project 
                { 
                    ProjectId = "proj2", 
                    ProjectCode = "VHM2025", 
                    Name = "Vinhomes Grand Park", 
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    Buildings = new List<Building>
                    {
                        new Building { BuildingId = "b2", BuildingCode = "B2", Name = "Building 2", IsActive = true },
                        new Building { BuildingId = "b3", BuildingCode = "B3", Name = "Building 3", IsActive = false }
                    }
                },
                new Project 
                { 
                    ProjectId = "proj3", 
                    ProjectCode = "OLD2020", 
                    Name = "Dự Án Cũ", 
                    IsActive = false, 
                    CreatedAt = DateTime.UtcNow.AddDays(-100) 
                }
            };

            _context.Projects.AddRange(projects);
            _context.SaveChanges();
        }

        // --- GetAllAsync Tests (15 Cases) ---

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldReturnAllProjects_WhenNoFilterProvided_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, null, null, null);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.Select(p => p.Name).Should().Contain(new[] { "Chung Cư Sunrise", "Vinhomes Grand Park", "Dự Án Cũ" });
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldReturnActiveProjects_WhenIsActiveTrue_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(true, null, null, null);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.All(p => p.IsActive).Should().BeTrue();
            result.Data.Select(p => p.ProjectCode).Should().Contain(new[] { "CCSR2025", "VHM2025" });
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldReturnInactiveProjects_WhenIsActiveFalse_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(false, null, null, null);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().Name.Should().Be("Dự Án Cũ");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldReturnMatchingProjects_WhenSearchByName_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, "Sunrise", null, null);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().Name.Should().Be("Chung Cư Sunrise");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldReturnMatchingProjects_WhenSearchByCode_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, "VHM", null, null);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().ProjectCode.Should().Be("VHM2025");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldReturnEmpty_WhenSearchTermDoesNotMatch_A()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, "KhongTonTai", null, null);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue(); // Returns success with empty list
            result.Data.Should().BeEmpty();
            result.Message.Should().Be("Không tìm thấy kết quả phù hợp."); // SM01_NO_RESULTS
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldSortByNameAsc_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, null, "name", "asc");

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            var names = result.Data.Select(p => p.Name).ToList();
            names.Should().BeInAscendingOrder();
            names[0].Should().Be("Chung Cư Sunrise");
            names[2].Should().Be("Vinhomes Grand Park");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldSortByNameDesc_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, null, "name", "desc");

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            var names = result.Data.Select(p => p.Name).ToList();
            names.Should().BeInDescendingOrder();
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldSortByNumApartmentsAsc_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, null, "numapartments", "asc");

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            // proj1: 1 rented, proj2: 0 rented, proj3: 0 rented
            result.Succeeded.Should().BeTrue();
            var counts = result.Data.Select(p => p.NumApartments).ToList();
            counts.Should().BeInAscendingOrder();
            result.Data.Last().ProjectCode.Should().Be("CCSR2025"); // Has 1 rented
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldSortByNumApartmentsDesc_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, null, "numapartments", "desc");

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            var counts = result.Data.Select(p => p.NumApartments).ToList();
            counts.Should().BeInDescendingOrder();
            result.Data.First().ProjectCode.Should().Be("CCSR2025");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldSortByNumBuildingsAsc_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, null, "numbuildings", "asc");

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            // proj1: 1 active bld, proj2: 1 active bld, proj3: 0
            result.Succeeded.Should().BeTrue();
            var counts = result.Data.Select(p => p.NumBuildings).ToList();
            counts.Should().BeInAscendingOrder();
            result.Data.First().ProjectCode.Should().Be("OLD2020"); // 0 buildings
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldSortByNumBuildingsDesc_N()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, null, "numbuildings", "desc");

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            var counts = result.Data.Select(p => p.NumBuildings).ToList();
            counts.Should().BeInDescendingOrder();
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldHandleSpecialCharactersInSearch_B()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, "@#$%", null, null);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldTrimSearchTerm_B()
        {
            // Arrange
            var query = new ProjectQueryParameters(null, "  Sunrise  ", null, null);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().Name.Should().Be("Chung Cư Sunrise");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAllAsync_ShouldReturnEmpty_WhenNoProjectsExist_A()
        {
            // Arrange
            _context.Projects.RemoveRange(_context.Projects);
            _context.SaveChanges();
            var query = new ProjectQueryParameters(null, null, null, null);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            result.Message.Should().Be("Không tìm thấy kết quả phù hợp.");
        }

        // --- GetByIdAsync Tests (15 Cases) ---

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnProject_WhenIdExists_N()
        {
            // Act
            var result = await _service.GetByIdAsync("proj1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ProjectId.Should().Be("proj1");
            result.Data.Name.Should().Be("Chung Cư Sunrise");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldMapFieldsCorrectly_N()
        {
            // Act
            var result = await _service.GetByIdAsync("proj1");

            // Assert
            result.Succeeded.Should().BeTrue();
            var data = result.Data;
            data.ProjectCode.Should().Be("CCSR2025");
            data.IsActive.Should().BeTrue();
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldCountRentedApartmentsCorrectly_N()
        {
            // Act
            var result = await _service.GetByIdAsync("proj1");

            // Assert
            // proj1 has 2 apts: 1 "Đã Thuê", 1 "Trống"
            result.Succeeded.Should().BeTrue();
            result.Data.NumApartments.Should().Be(1);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldCountActiveBuildingsCorrectly_N()
        {
            // Act
            var result = await _service.GetByIdAsync("proj2");

            // Assert
            // proj2 has 2 blds: 1 Active, 1 Inactive
            result.Succeeded.Should().BeTrue();
            result.Data.NumBuildings.Should().Be(1);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnFail_WhenIdDoesNotExist_A()
        {
            // Act
            var result = await _service.GetByIdAsync("nonexistent");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy kết quả phù hợp.");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnFail_WhenIdIsNull_A()
        {
            // Act
            var result = await _service.GetByIdAsync(null);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy kết quả phù hợp.");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnFail_WhenIdIsEmpty_A()
        {
            // Act
            var result = await _service.GetByIdAsync("");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy kết quả phù hợp.");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnFail_WhenIdHasSpecialChars_B()
        {
            // Act
            var result = await _service.GetByIdAsync("@#$");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy kết quả phù hợp.");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnInactiveProject_N()
        {
            // Act
            var result = await _service.GetByIdAsync("proj3");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.IsActive.Should().BeFalse();
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnZeroBuildings_WhenNoneExist_N()
        {
            // Act
            var result = await _service.GetByIdAsync("proj3");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.NumBuildings.Should().Be(0);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnZeroApartments_WhenNoneExist_N()
        {
            // Act
            var result = await _service.GetByIdAsync("proj3");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.NumApartments.Should().Be(0);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnZeroApartments_WhenNoneRented_N()
        {
            // Arrange: Add a project with apartments but none rented
            var proj = new Project 
            { 
                ProjectId = "proj4", 
                ProjectCode = "TEST4", 
                Name = "Test 4",
                Buildings = new List<Building>
                {
                    new Building 
                    { 
                        BuildingId = "b4", 
                        BuildingCode = "B4",
                        Name = "Building 4",
                        IsActive = true,
                        Apartments = new List<Apartment>
                        {
                            new Apartment { ApartmentId = "a4", Code = "A401", Status = "Trống" }
                        }
                    }
                }
            };
            _context.Projects.Add(proj);
            _context.SaveChanges();

            // Act
            var result = await _service.GetByIdAsync("proj4");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.NumApartments.Should().Be(0);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnPayOSInfo_WhenAvailable_N()
        {
            // Arrange
            var proj = new Project 
            { 
                ProjectId = "proj5", 
                ProjectCode = "PAYOS", 
                Name = "PayOS Project",
                PayOSClientId = "client1",
                PayOSApiKey = "api1",
                PayOSChecksumKey = "check1"
            };
            _context.Projects.Add(proj);
            _context.SaveChanges();

            // Act
            var result = await _service.GetByIdAsync("proj5");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.PayOSClientId.Should().Be("client1");
            result.Data.PayOSApiKey.Should().Be("api1");
            result.Data.PayOSChecksumKey.Should().Be("check1");
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldReturnNullPayOSInfo_WhenNotSet_N()
        {
            // Act
            var result = await _service.GetByIdAsync("proj1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.PayOSClientId.Should().BeNull();
        }

        [Fact]
        public async System.Threading.Tasks.Task GetByIdAsync_ShouldHandleLongId_B()
        {
            // Arrange
            var longId = new string('a', 50);
            // Act
            var result = await _service.GetByIdAsync(longId);

            // Assert
            result.Succeeded.Should().BeFalse(); // Just checks it doesn't crash
            result.Message.Should().Be("Không tìm thấy kết quả phù hợp.");
        }

        // --- CreateAsync Tests (15 Cases) ---

        private void SetupMapper()
        {
            _mockMapper.Setup(m => m.Map<Project>(It.IsAny<ProjectCreateDto>()))
                .Returns((ProjectCreateDto dto) => new Project 
                { 
                    Name = dto.Name,
                    ProjectCode = dto.ProjectCode,
                    Address = dto.Address,
                    Ward = dto.Ward,
                    District = dto.District,
                    City = dto.City,
                    BankAccountNumber = dto.BankAccountNumber,
                    BankAccountName = dto.BankAccountName,
                    PayOSClientId = dto.PayOSClientId,
                    PayOSApiKey = dto.PayOSApiKey,
                    PayOSChecksumKey = dto.PayOSChecksumKey
                });
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldCreateProject_WhenValid_N()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "NEW2025", "New Project", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Name.Should().Be("New Project");
            result.Data.ProjectCode.Should().Be("NEW2025");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldConvertNameToTitleCase_N()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "CCM2025", "chung cư mới", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Name.Should().Be("Chung Cư Mới");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldConvertAddressToTitleCase_N()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "TEST", "Test", "123 đường láng", "phường láng thượng", "quận đống đa", "hà nội", null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Address.Should().Be("123 Đường Láng");
            result.Data.Ward.Should().Be("Phường Láng Thượng");
            result.Data.District.Should().Be("Quận Đống Đa");
            result.Data.City.Should().Be("Hà Nội");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldConvertBankAccountNameToUpperCase_N()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "TEST", "Test", null, null, null, null, null, null, "nguyen van a", null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.BankAccountName.Should().Be("NGUYEN VAN A");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldSetIsActiveTrue_N()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "TEST", "Test", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.IsActive.Should().BeTrue();
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenProjectCodeExists_A()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "CCSR2025", "Duplicate", null, null, null, null, null, null, null, null, null, null
            ); // Exists in Seed

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("ProjectCode đã tồn tại. Vui lòng sử dụng giá trị khác."); // Duplicate Code
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenProjectCodeInvalidChars_A()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "Code@123", "Invalid", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Mã dự án không hợp lệ (Chỉ A-Z, 0-9, _)."); // Invalid Input
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenProjectCodeTooShort_B()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "A", "Short", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Mã dự án phải từ 2 đến 50 ký tự.");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenNameEmpty_A()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "TEST", "", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Tên dự án không được để trống.");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenNameTooShort_B()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "TEST", "Ab", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Tên dự án phải từ 3 đến 200 ký tự.");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenBankAccountNumberInvalid_A()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "TEST", "Test", null, null, null, null, null, "123abc456", null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Số tài khoản ngân hàng không hợp lệ (chỉ được chứa số).");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenPayOSInfoIncomplete_A()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "TEST", "Test", null, null, null, null, null, null, null, "client", "api", null
            );
            // Missing ChecksumKey

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Nếu cấu hình PayOS, vui lòng nhập đầy đủ 3 thông tin: Client ID, API Key và Checksum Key.");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldSucceed_WhenPayOSInfoComplete_N()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "TEST", "Test", null, null, null, null, null, null, null, "client", "api", "check"
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.PayOSClientId.Should().Be("client");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldTrimProjectCode_N()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "  TRIMMED  ", "Test", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.ProjectCode.Should().Be("TRIMMED");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldHandleVietnameseCharactersInName_N()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "VN2025", "Dự Án Tiếng Việt", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Name.Should().Be("Dự Án Tiếng Việt");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenProjectCodeTooLong_B()
        {
            // Arrange
            SetupMapper();
            var longCode = new string('A', 51);
            var dto = new ProjectCreateDto(
                longCode, "Valid Name", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Mã dự án phải từ 2 đến 50 ký tự.");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenNameTooLong_B()
        {
            // Arrange
            SetupMapper();
            var longName = new string('A', 201);
            var dto = new ProjectCreateDto(
                "CODE", longName, null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Tên dự án phải từ 3 đến 200 ký tự.");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenBankAccountNumberTooShort_B()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                "CODE", "Valid Name", null, null, null, null, null, "12345", null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Số tài khoản ngân hàng phải từ 6 đến 25 chữ số.");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenBankAccountNumberTooLong_B()
        {
            // Arrange
            SetupMapper();
            var longBank = new string('1', 26);
            var dto = new ProjectCreateDto(
                "CODE", "Valid Name", null, null, null, null, null, longBank, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Số tài khoản ngân hàng phải từ 6 đến 25 chữ số.");
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAsync_ShouldFail_WhenProjectCodeIsNull_A()
        {
            // Arrange
            SetupMapper();
            var dto = new ProjectCreateDto(
                null, "Valid Name", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.CreateAsync(dto, "admin1");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Mã dự án không được để trống.");
        }

        // --- UpdateAsync Tests (15 Cases) ---

        private void SetupUpdateMapper()
        {
            _mockMapper.Setup(m => m.Map(It.IsAny<ProjectUpdateDto>(), It.IsAny<Project>()))
                .Callback((ProjectUpdateDto dto, Project entity) => 
                {
                    if (dto.Name != null) entity.Name = dto.Name;
                    if (dto.Address != null) entity.Address = dto.Address;
                    if (dto.Ward != null) entity.Ward = dto.Ward;
                    if (dto.District != null) entity.District = dto.District;
                    if (dto.City != null) entity.City = dto.City;
                    if (dto.BankAccountNumber != null) entity.BankAccountNumber = dto.BankAccountNumber;
                    if (dto.BankAccountName != null) entity.BankAccountName = dto.BankAccountName;
                    if (dto.PayOSClientId != null) entity.PayOSClientId = dto.PayOSClientId;
                    if (dto.PayOSApiKey != null) entity.PayOSApiKey = dto.PayOSApiKey;
                    if (dto.PayOSChecksumKey != null) entity.PayOSChecksumKey = dto.PayOSChecksumKey;
                    if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
                });
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldUpdateName_WhenValid_N()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                "Tên Mới", null, null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            var proj = await _context.Projects.FindAsync("proj1");
            proj.Name.Should().Be("Tên Mới"); // TitleCase applied
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldUpdateAddressToTitleCase_N()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                null, "đường mới", null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            var proj = await _context.Projects.FindAsync("proj1");
            proj.Address.Should().Be("Đường Mới");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldUpdateBankAccountNameToUpperCase_N()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, "nguyen van b", null, null, null, null
            );

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            var proj = await _context.Projects.FindAsync("proj1");
            proj.BankAccountName.Should().Be("NGUYEN VAN B");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldUpdatePayOSInfo_N()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, null, "newClient", "newApi", "newCheck", null
            );

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            var proj = await _context.Projects.FindAsync("proj1");
            proj.PayOSClientId.Should().Be("newClient");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldDeactivateProject_N()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, null, null, null, null, false
            );

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            var proj = await _context.Projects.FindAsync("proj1");
            proj.IsActive.Should().BeFalse();
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldCancelActiveSubscriptions_WhenDeactivating_N()
        {
            // Arrange
            SetupUpdateMapper();
            var sub = new Subscription 
            { 
                SubscriptionId = "sub1", 
                SubscriptionCode = "SUB001",
                ProjectId = "proj1", 
                Status = "Active", 
                ExpiredAt = DateTime.UtcNow.AddDays(30) 
            };
            _context.Subscriptions.Add(sub);
            _context.SaveChanges();

            // Mock SubRepo to find this sub
            _mockSubRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Subscription, bool>>>()))
                .ReturnsAsync(sub);

            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, null, null, null, null, false
            );

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            sub.Status.Should().Be("Cancelled");
            sub.ExpiredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldDeactivateResidents_WhenDeactivating_N()
        {
            // Arrange
            SetupUpdateMapper();
            var user = new User 
            { 
                UserId = "u1", 
                Name = "User 1",
                PasswordHash = "hash",
                RoleId = "Resident",
                Status = "Active",
                Apartment = new Apartment 
                { 
                    ApartmentId = "apt_u1",
                    Code = "A_U1",
                    Status = "Active",
                    Building = new Building { BuildingId = "b_u1", ProjectId = "proj1", BuildingCode = "B_U1", Name = "Building U1" } 
                }
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, null, null, null, null, false
            );

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            var updatedUser = await _context.Users.FindAsync("u1");
            updatedUser.Status.Should().Be("Inactive");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldDeactivateStaffAssignments_WhenDeactivating_N()
        {
            // Arrange
            SetupUpdateMapper();
            var assignment = new StaffBuildingAssignment 
            { 
                AssignmentId = "assign1",
                UserId = "staff1",
                IsActive = true,
                Building = new Building { BuildingId = "b_a1", ProjectId = "proj1", BuildingCode = "B_A1", Name = "Building A1" }
            };
            _context.StaffBuildingAssignments.Add(assignment);
            _context.SaveChanges();

            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, null, null, null, null, false
            );

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            var updatedAssign = await _context.StaffBuildingAssignments.FindAsync("assign1");
            updatedAssign.IsActive.Should().BeFalse();
            updatedAssign.AssignmentEndDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldReactivateProject_N()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, null, null, null, null, true
            );

            // Act
            var result = await _service.UpdateAsync("proj3", dto); // proj3 is Inactive

            // Assert
            result.Succeeded.Should().BeTrue();
            var proj = await _context.Projects.FindAsync("proj3");
            proj.IsActive.Should().BeTrue();
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldReactivateResidents_WhenReactivating_N()
        {
            // Arrange
            SetupUpdateMapper();
            var user = new User 
            { 
                UserId = "u2", 
                Name = "User 2",
                PasswordHash = "hash",
                RoleId = "Resident",
                Status = "Inactive",
                Apartment = new Apartment 
                { 
                    ApartmentId = "apt_u2",
                    Code = "A_U2",
                    Status = "Inactive",
                    Building = new Building { BuildingId = "b_u2", ProjectId = "proj3", BuildingCode = "B_U2", Name = "Building U2" } 
                }
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, null, null, null, null, true
            );

            // Act
            var result = await _service.UpdateAsync("proj3", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            var updatedUser = await _context.Users.FindAsync("u2");
            updatedUser.Status.Should().Be("Active");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldNotReactivateStaffAssignments_WhenReactivating_N()
        {
            // Arrange
            SetupUpdateMapper();
            var assignment = new StaffBuildingAssignment 
            { 
                AssignmentId = "assign2",
                UserId = "staff2",
                IsActive = false,
                Building = new Building { BuildingId = "b_a2", ProjectId = "proj3", BuildingCode = "B_A2", Name = "Building A2" }
            };
            _context.StaffBuildingAssignments.Add(assignment);
            _context.SaveChanges();

            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, null, null, null, null, true
            );

            // Act
            var result = await _service.UpdateAsync("proj3", dto);

            // Assert
            result.Succeeded.Should().BeTrue();
            var updatedAssign = await _context.StaffBuildingAssignments.FindAsync("assign2");
            updatedAssign.IsActive.Should().BeFalse(); // Should remain false
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldFail_WhenProjectNotFound_A()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                "New Name", null, null, null, null, null, null, null, null, null, null, null
            );

            // Act
            var result = await _service.UpdateAsync("nonexistent", dto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy kết quả phù hợp.");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldFail_WhenNameInvalid_A()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                "Ab", null, null, null, null, null, null, null, null, null, null, null
            ); // Too short

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Tên dự án phải từ 3 đến 200 ký tự.");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldFail_WhenBankAccountInvalid_A()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, "abc", null, null, null, null, null
            );

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Số tài khoản ngân hàng không hợp lệ (chỉ được chứa số).");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateAsync_ShouldFail_WhenPayOSInfoIncomplete_A()
        {
            // Arrange
            SetupUpdateMapper();
            var dto = new ProjectUpdateDto(
                null, null, null, null, null, null, null, null, "client", null, null, null
            );
            // Missing others

            // Act
            var result = await _service.UpdateAsync("proj1", dto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Nếu cấu hình PayOS, vui lòng nhập đầy đủ 3 thông tin: Client ID, API Key và Checksum Key.");
        }

    }
}

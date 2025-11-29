using Xunit;
using Moq;
using FluentAssertions;
using ApartaAPI.Services;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Models;
using ApartaAPI.DTOs.Vehicles;
using ApartaAPI.DTOs.Common;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.TestVehicle
{
    /// <summary>
    /// Test suite cho VehicleService - Bao gồm các test case đầy đủ và dễ tái sử dụng
    /// </summary>
    public class VehicleServiceTests
    {
        private readonly Mock<IRepository<Vehicle>> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly VehicleService _service;

        public VehicleServiceTests()
        {
            _repositoryMock = new Mock<IRepository<Vehicle>>();
            _mapperMock = new Mock<IMapper>();
            _service = new VehicleService(_repositoryMock.Object, _mapperMock.Object);
        }

        #region Helper Methods - Dễ dàng tái sử dụng

        /// <summary>
        /// Tạo Vehicle mẫu để test
        /// </summary>
        private Vehicle CreateSampleVehicle(string vehicleId = "vehicle-001", string apartmentId = "apt-001",
            string vehicleNumber = "29A-12345", string? info = "Honda City", string status = "Chờ duyệt")
        {
            return new Vehicle
            {
                VehicleId = vehicleId,
                ApartmentId = apartmentId,
                VehicleNumber = vehicleNumber,
                Info = info,
                Status = status,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
        }

        /// <summary>
        /// Tạo VehicleDto mẫu để test
        /// </summary>
        private VehicleDto CreateSampleVehicleDto(string vehicleId = "vehicle-001", string apartmentId = "apt-001",
            string vehicleNumber = "29A-12345", string? info = "Honda City", string status = "Chờ duyệt")
        {
            return new VehicleDto(
                VehicleId: vehicleId,
                ApartmentId: apartmentId,
                VehicleNumber: vehicleNumber,
                Info: info,
                Status: status,
                CreatedAt: DateTime.UtcNow.AddDays(-7)
            );
        }

        /// <summary>
        /// Tạo VehicleCreateDto mẫu để test
        /// </summary>
        private VehicleCreateDto CreateSampleVehicleCreateDto(string apartmentId = "apt-001",
            string vehicleNumber = "29A-12345", string? info = "Honda City", string status = "Chờ duyệt")
        {
            return new VehicleCreateDto(
                ApartmentId: apartmentId,
                VehicleNumber: vehicleNumber,
                Info: info,
                Status: status
            );
        }

        /// <summary>
        /// Tạo VehicleUpdateDto mẫu để test
        /// </summary>
        private VehicleUpdateDto CreateSampleVehicleUpdateDto(string? vehicleNumber = "29A-99999",
            string? info = "Updated Vehicle", string? status = "Đã duyệt")
        {
            return new VehicleUpdateDto(
                VehicleNumber: vehicleNumber,
                Info: info,
                Status: status
            );
        }

        /// <summary>
        /// Tạo danh sách Vehicles mẫu để test
        /// </summary>
        private List<Vehicle> CreateSampleVehicleList()
        {
            return new List<Vehicle>
            {
                CreateSampleVehicle("vehicle-001", "apt-001", "29A-12345", "Honda City", "Chờ duyệt"),
                CreateSampleVehicle("vehicle-002", "apt-001", "30B-67890", "Toyota Vios", "Đã duyệt"),
                CreateSampleVehicle("vehicle-003", "apt-002", "51C-11111", "Mazda 3", "Chờ duyệt"),
                CreateSampleVehicle("vehicle-004", "apt-002", "43D-22222", "Honda Wave", "Đã duyệt")
            };
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_WithNullQuery_ShouldReturnAllVehicles()
        {
            // Arrange
            var vehicles = CreateSampleVehicleList();
            var vehicleDtos = vehicles.Select(v => CreateSampleVehicleDto(v.VehicleId, v.ApartmentId, v.VehicleNumber, v.Info, v.Status)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(vehicleDtos);

            // Act
            var result = await _service.GetAllAsync(null!);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Message.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_WithStatusFilter_ShouldReturnFilteredVehicles()
        {
            // Arrange
            var query = new VehicleQueryParameters("Chờ duyệt", null, null, null);
            var vehicles = CreateSampleVehicleList().Where(v => v.Status.ToLower() == "chờ duyệt").ToList();
            var vehicleDtos = vehicles.Select(v => CreateSampleVehicleDto(v.VehicleId, v.ApartmentId, v.VehicleNumber, v.Info, v.Status)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(vehicleDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(v => v.Status == "Chờ duyệt");
        }

        [Fact]
        public async Task GetAllAsync_WithSearchTerm_VehicleNumber_ShouldReturnMatchingVehicles()
        {
            // Arrange
            var query = new VehicleQueryParameters(null, "29A", null, null);
            var vehicles = CreateSampleVehicleList().Where(v => v.VehicleNumber.ToLower().Contains("29a")).ToList();
            var vehicleDtos = vehicles.Select(v => CreateSampleVehicleDto(v.VehicleId, v.ApartmentId, v.VehicleNumber, v.Info, v.Status)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(vehicleDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.First().VehicleNumber.Should().Contain("29A");
        }

        [Fact]
        public async Task GetAllAsync_WithSearchTerm_ApartmentId_ShouldReturnMatchingVehicles()
        {
            // Arrange
            var query = new VehicleQueryParameters(null, "apt-001", null, null);
            var vehicles = CreateSampleVehicleList().Where(v => v.ApartmentId.ToLower().Contains("apt-001")).ToList();
            var vehicleDtos = vehicles.Select(v => CreateSampleVehicleDto(v.VehicleId, v.ApartmentId, v.VehicleNumber, v.Info, v.Status)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(vehicleDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(v => v.ApartmentId == "apt-001");
        }

        [Fact]
        public async Task GetAllAsync_WithSearchTerm_Info_ShouldReturnMatchingVehicles()
        {
            // Arrange
            var query = new VehicleQueryParameters(null, "honda", null, null);
            var vehicles = CreateSampleVehicleList().Where(v => v.Info != null && v.Info.ToLower().Contains("honda")).ToList();
            var vehicleDtos = vehicles.Select(v => CreateSampleVehicleDto(v.VehicleId, v.ApartmentId, v.VehicleNumber, v.Info, v.Status)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(vehicleDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(v => v.Info!.ToLower().Contains("honda"));
        }

        [Fact]
        public async Task GetAllAsync_WithSortByVehicleNumber_Ascending_ShouldReturnSortedVehicles()
        {
            // Arrange
            var query = new VehicleQueryParameters(null, null, "vehiclenumber", "asc");
            var vehicles = CreateSampleVehicleList();
            var sortedVehicles = vehicles.OrderBy(v => v.VehicleNumber).ToList();
            var vehicleDtos = sortedVehicles.Select(v => CreateSampleVehicleDto(v.VehicleId, v.ApartmentId, v.VehicleNumber, v.Info, v.Status)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(vehicleDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Data.First().VehicleNumber.Should().Be("29A-12345");
        }

        [Fact]
        public async Task GetAllAsync_WithSortByVehicleNumber_Descending_ShouldReturnSortedVehicles()
        {
            // Arrange
            var query = new VehicleQueryParameters(null, null, "vehiclenumber", "desc");
            var vehicles = CreateSampleVehicleList();
            var sortedVehicles = vehicles.OrderByDescending(v => v.VehicleNumber).ToList();
            var vehicleDtos = sortedVehicles.Select(v => CreateSampleVehicleDto(v.VehicleId, v.ApartmentId, v.VehicleNumber, v.Info, v.Status)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(vehicleDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Data.First().VehicleNumber.Should().Be("51C-11111");
        }

        [Fact]
        public async Task GetAllAsync_WithNoSortBy_ShouldSortByCreatedAtDescending()
        {
            // Arrange
            var query = new VehicleQueryParameters(null, null, null, null);
            var vehicles = CreateSampleVehicleList();

            // Set different CreatedAt dates for testing
            vehicles[0].CreatedAt = DateTime.UtcNow.AddDays(-10);
            vehicles[1].CreatedAt = DateTime.UtcNow.AddDays(-5);
            vehicles[2].CreatedAt = DateTime.UtcNow.AddDays(-2);
            vehicles[3].CreatedAt = DateTime.UtcNow.AddDays(-1);

            var sortedVehicles = vehicles.OrderByDescending(v => v.CreatedAt).ToList();
            var vehicleDtos = sortedVehicles.Select(v => CreateSampleVehicleDto(v.VehicleId, v.ApartmentId, v.VehicleNumber, v.Info, v.Status)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(vehicleDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
        }

        [Fact]
        public async Task GetAllAsync_WhenNoVehiclesFound_ShouldReturnEmptyListWithNoResultsMessage()
        {
            // Arrange
            var query = new VehicleQueryParameters("non-existent-status", null, null, null);
            var emptyList = new List<Vehicle>();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(emptyList);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(new List<VehicleDto>());

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            result.Message.Should().Be(ApiResponse.SM01_NO_RESULTS);
        }

        [Fact]
        public async Task GetAllAsync_WhenRepositoryReturnsNull_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new VehicleQueryParameters(null, null, null, null);

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync((List<Vehicle>)null!);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(new List<VehicleDto>());

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            result.Message.Should().Be(ApiResponse.SM01_NO_RESULTS);
        }

        [Fact]
        public async Task GetAllAsync_WithCombinedFilters_ShouldReturnFilteredAndSortedVehicles()
        {
            // Arrange
            var query = new VehicleQueryParameters("Đã duyệt", "apt-002", "vehiclenumber", "asc");
            var vehicles = CreateSampleVehicleList()
                .Where(v => v.Status.ToLower() == "đã duyệt" && v.ApartmentId == "apt-002")
                .OrderBy(v => v.VehicleNumber)
                .ToList();
            var vehicleDtos = vehicles.Select(v => CreateSampleVehicleDto(v.VehicleId, v.ApartmentId, v.VehicleNumber, v.Info, v.Status)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicles);
            _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
                .Returns(vehicleDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().OnlyContain(v => v.Status == "Đã duyệt" && v.ApartmentId == "apt-002");
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WhenVehicleExists_ShouldReturnVehicleDto()
        {
            // Arrange
            var vehicleId = "vehicle-001";
            var vehicle = CreateSampleVehicle(vehicleId);
            var vehicleDto = CreateSampleVehicleDto(vehicleId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(vehicle);
            _mapperMock.Setup(m => m.Map<VehicleDto?>(vehicle))
                .Returns(vehicleDto);

            // Act
            var result = await _service.GetByIdAsync(vehicleId);

            // Assert
            result.Should().NotBeNull();
            result!.VehicleId.Should().Be(vehicleId);
            _repositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenVehicleNotFound_ShouldReturnNull()
        {
            // Arrange
            var vehicleId = "non-existent-vehicle";

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync((Vehicle)null!);
            _mapperMock.Setup(m => m.Map<VehicleDto?>(null))
                .Returns((VehicleDto)null!);

            // Act
            var result = await _service.GetByIdAsync(vehicleId);

            // Assert
            result.Should().BeNull();
            _repositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()), Times.Once);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidDto_ShouldCreateVehicle()
        {
            // Arrange
            var createDto = CreateSampleVehicleCreateDto();
            var vehicle = new Vehicle
            {
                ApartmentId = createDto.ApartmentId,
                VehicleNumber = createDto.VehicleNumber,
                Info = createDto.Info,
                Status = createDto.Status
            };
            var vehicleDto = CreateSampleVehicleDto();

            _mapperMock.Setup(m => m.Map<Vehicle>(createDto))
                .Returns(vehicle);
            _mapperMock.Setup(m => m.Map<VehicleDto>(It.IsAny<Vehicle>()))
                .Returns(vehicleDto);
            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync((Vehicle)null!); // No duplicate
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Vehicle>()))
                .ReturnsAsync((Vehicle v) => v);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.VehicleId.Should().NotBeNullOrEmpty();
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Vehicle>(v =>
                !string.IsNullOrEmpty(v.VehicleId) &&
                v.CreatedAt.HasValue &&
                v.UpdatedAt.HasValue &&
                v.Status == "Chờ duyệt"
            )), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateVehicleNumber_ShouldThrowException()
        {
            // Arrange
            var createDto = CreateSampleVehicleCreateDto();
            var vehicle = new Vehicle
            {
                ApartmentId = createDto.ApartmentId,
                VehicleNumber = createDto.VehicleNumber,
                Info = createDto.Info,
                Status = createDto.Status
            };
            var existingVehicle = CreateSampleVehicle(vehicleNumber: createDto.VehicleNumber);

            _mapperMock.Setup(m => m.Map<Vehicle>(createDto))
                .Returns(vehicle);
            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(existingVehicle); // Duplicate found

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldSetCreatedAtAndUpdatedAt()
        {
            // Arrange
            var createDto = CreateSampleVehicleCreateDto();
            var vehicle = new Vehicle
            {
                ApartmentId = createDto.ApartmentId,
                VehicleNumber = createDto.VehicleNumber,
                Info = createDto.Info,
                Status = createDto.Status
            };
            var vehicleDto = CreateSampleVehicleDto();

            _mapperMock.Setup(m => m.Map<Vehicle>(createDto))
                .Returns(vehicle);
            _mapperMock.Setup(m => m.Map<VehicleDto>(It.IsAny<Vehicle>()))
                .Returns(vehicleDto);
            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync((Vehicle)null!);
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Vehicle>()))
                .ReturnsAsync((Vehicle v) => v);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Vehicle>(v =>
                v.CreatedAt.HasValue &&
                v.UpdatedAt.HasValue &&
                (v.UpdatedAt.Value - v.CreatedAt.Value).TotalSeconds < 1
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldGenerateUniqueVehicleIdAndSetStatusToChoDuyet()
        {
            // Arrange
            var createDto = CreateSampleVehicleCreateDto();
            var vehicle = new Vehicle
            {
                ApartmentId = createDto.ApartmentId,
                VehicleNumber = createDto.VehicleNumber,
                Info = createDto.Info,
                Status = createDto.Status
            };
            var vehicleDto = CreateSampleVehicleDto();

            _mapperMock.Setup(m => m.Map<Vehicle>(createDto))
                .Returns(vehicle);
            _mapperMock.Setup(m => m.Map<VehicleDto>(It.IsAny<Vehicle>()))
                .Returns(vehicleDto);
            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync((Vehicle)null!);
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Vehicle>()))
                .ReturnsAsync((Vehicle v) => v);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Vehicle>(v =>
                !string.IsNullOrEmpty(v.VehicleId) &&
                v.VehicleId.Length == 32 && // GUID with "N" format
                v.Status == "Chờ duyệt"
            )), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WhenVehicleExists_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var vehicleId = "vehicle-001";
            var updateDto = CreateSampleVehicleUpdateDto();
            var existingVehicle = CreateSampleVehicle(vehicleId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(existingVehicle);
            _mapperMock.Setup(m => m.Map(updateDto, existingVehicle))
                .Returns(existingVehicle);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>()))
                .ReturnsAsync((Vehicle v) => v);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(vehicleId, updateDto);

            // Assert
            result.Should().BeTrue();
            _repositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(existingVehicle), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenVehicleExists_ShouldUpdateUpdatedAt()
        {
            // Arrange
            var vehicleId = "vehicle-001";
            var updateDto = CreateSampleVehicleUpdateDto();
            var existingVehicle = CreateSampleVehicle(vehicleId);
            var oldUpdatedAt = existingVehicle.UpdatedAt;

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(existingVehicle);
            _mapperMock.Setup(m => m.Map(updateDto, existingVehicle))
                .Returns(existingVehicle);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>()))
                .ReturnsAsync((Vehicle v) => v);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _service.UpdateAsync(vehicleId, updateDto);

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Vehicle>(v =>
                v.UpdatedAt > oldUpdatedAt &&
                v.UpdatedAt.Value <= DateTime.UtcNow
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenVehicleNotFound_ShouldReturnFalse()
        {
            // Arrange
            var vehicleId = "non-existent-vehicle";
            var updateDto = CreateSampleVehicleUpdateDto();

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync((Vehicle)null!);

            // Act
            var result = await _service.UpdateAsync(vehicleId, updateDto);

            // Assert
            result.Should().BeFalse();
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var vehicleId = "vehicle-001";
            var updateDto = CreateSampleVehicleUpdateDto();
            var existingVehicle = CreateSampleVehicle(vehicleId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(existingVehicle);
            _mapperMock.Setup(m => m.Map(updateDto, existingVehicle))
                .Returns(existingVehicle);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>()))
                .ReturnsAsync((Vehicle v) => v);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateAsync(vehicleId, updateDto);

            // Assert
            result.Should().BeFalse();
            _repositoryMock.Verify(r => r.UpdateAsync(existingVehicle), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WhenVehicleExists_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            var vehicleId = "vehicle-001";
            var existingVehicle = CreateSampleVehicle(vehicleId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(existingVehicle);
            _repositoryMock.Setup(r => r.RemoveAsync(It.IsAny<Vehicle>()))
                .Returns(Task.CompletedTask);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(vehicleId);

            // Assert
            result.Should().BeTrue();
            _repositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()), Times.Once);
            _repositoryMock.Verify(r => r.RemoveAsync(existingVehicle), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenVehicleNotFound_ShouldReturnFalse()
        {
            // Arrange
            var vehicleId = "non-existent-vehicle";

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync((Vehicle)null!);

            // Act
            var result = await _service.DeleteAsync(vehicleId);

            // Assert
            result.Should().BeFalse();
            _repositoryMock.Verify(r => r.RemoveAsync(It.IsAny<Vehicle>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var vehicleId = "vehicle-001";
            var existingVehicle = CreateSampleVehicle(vehicleId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Vehicle, bool>>>()))
                .ReturnsAsync(existingVehicle);
            _repositoryMock.Setup(r => r.RemoveAsync(It.IsAny<Vehicle>()))
                .Returns(Task.CompletedTask);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAsync(vehicleId);

            // Assert
            result.Should().BeFalse();
            _repositoryMock.Verify(r => r.RemoveAsync(existingVehicle), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}

using Xunit;
using Moq;
using FluentAssertions;
using ApartaAPI.Services;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Models;
using ApartaAPI.DTOs.Assets;
using ApartaAPI.DTOs.Common;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.TestAsset
{
    /// <summary>
    /// Test suite cho AssetService - Bao gồm các test case đầy đủ và dễ tái sử dụng
    /// </summary>
    public class AssetServiceTests
    {
        private readonly Mock<IRepository<Asset>> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly AssetService _service;

        public AssetServiceTests()
        {
            _repositoryMock = new Mock<IRepository<Asset>>();
            _mapperMock = new Mock<IMapper>();
            _service = new AssetService(_repositoryMock.Object, _mapperMock.Object);
        }

        #region Helper Methods - Dễ dàng tái sử dụng

        /// <summary>
        /// Tạo Asset mẫu để test
        /// </summary>
        private Asset CreateSampleAsset(string assetId = "asset-001", string buildingId = "building-001", 
            string info = "Sample Asset", int quantity = 10)
        {
            return new Asset
            {
                AssetId = assetId,
                BuildingId = buildingId,
                Info = info,
                Quantity = quantity,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
        }

        /// <summary>
        /// Tạo AssetDto mẫu để test
        /// </summary>
        private AssetDto CreateSampleAssetDto(string assetId = "asset-001", string buildingId = "building-001",
            string info = "Sample Asset", int quantity = 10)
        {
            return new AssetDto
            {
                AssetId = assetId,
                BuildingId = buildingId,
                Info = info,
                Quantity = quantity,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            };
        }

        /// <summary>
        /// Tạo AssetCreateDto mẫu để test
        /// </summary>
        private AssetCreateDto CreateSampleAssetCreateDto(string buildingId = "building-001",
            string info = "New Asset", int quantity = 5)
        {
            return new AssetCreateDto
            {
                BuildingId = buildingId,
                Info = info,
                Quantity = quantity
            };
        }

        /// <summary>
        /// Tạo AssetUpdateDto mẫu để test
        /// </summary>
        private AssetUpdateDto CreateSampleAssetUpdateDto(string? info = "Updated Asset", int? quantity = 15)
        {
            return new AssetUpdateDto
            {
                Info = info,
                Quantity = quantity
            };
        }

        /// <summary>
        /// Tạo danh sách Assets mẫu để test
        /// </summary>
        private List<Asset> CreateSampleAssetList()
        {
            return new List<Asset>
            {
                CreateSampleAsset("asset-001", "building-001", "Bàn ghế văn phòng", 10),
                CreateSampleAsset("asset-002", "building-001", "Máy tính", 5),
                CreateSampleAsset("asset-003", "building-002", "Điều hòa", 8),
                CreateSampleAsset("asset-004", "building-002", "Bàn ghế phòng họp", 15)
            };
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_WithNullQuery_ShouldReturnAllAssets()
        {
            // Arrange
            var assets = CreateSampleAssetList();
            var assetDtos = assets.Select(a => CreateSampleAssetDto(a.AssetId, a.BuildingId, a.Info!, a.Quantity)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(assets);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(assetDtos);

            // Act
            var result = await _service.GetAllAsync(null!);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Message.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_WithBuildingIdFilter_ShouldReturnFilteredAssets()
        {
            // Arrange
            var query = new AssetQueryParameters("building-001", null, null, null);
            var assets = CreateSampleAssetList().Where(a => a.BuildingId == "building-001").ToList();
            var assetDtos = assets.Select(a => CreateSampleAssetDto(a.AssetId, a.BuildingId, a.Info!, a.Quantity)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(assets);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(assetDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(a => a.BuildingId == "building-001");
        }

        [Fact]
        public async Task GetAllAsync_WithSearchTerm_ShouldReturnMatchingAssets()
        {
            // Arrange
            var query = new AssetQueryParameters(null, "bàn ghế", null, null);
            var assets = CreateSampleAssetList().Where(a => a.Info!.ToLower().Contains("bàn ghế")).ToList();
            var assetDtos = assets.Select(a => CreateSampleAssetDto(a.AssetId, a.BuildingId, a.Info!, a.Quantity)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(assets);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(assetDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(a => a.Info!.ToLower().Contains("bàn ghế"));
        }

        [Fact]
        public async Task GetAllAsync_WithSortByInfo_Ascending_ShouldReturnSortedAssets()
        {
            // Arrange
            var query = new AssetQueryParameters(null, null, "info", "asc");
            var assets = CreateSampleAssetList();
            var sortedAssets = assets.OrderBy(a => a.Info).ToList();
            var assetDtos = sortedAssets.Select(a => CreateSampleAssetDto(a.AssetId, a.BuildingId, a.Info!, a.Quantity)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(assets);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(assetDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Data.First().Info.Should().Be("Bàn ghế phòng họp");
        }

        [Fact]
        public async Task GetAllAsync_WithSortByInfo_Descending_ShouldReturnSortedAssets()
        {
            // Arrange
            var query = new AssetQueryParameters(null, null, "info", "desc");
            var assets = CreateSampleAssetList();
            var sortedAssets = assets.OrderByDescending(a => a.Info).ToList();
            var assetDtos = sortedAssets.Select(a => CreateSampleAssetDto(a.AssetId, a.BuildingId, a.Info!, a.Quantity)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(assets);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(assetDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Data.First().Info.Should().Be("Máy tính");
        }

        [Fact]
        public async Task GetAllAsync_WithSortByQuantity_Ascending_ShouldReturnSortedAssets()
        {
            // Arrange
            var query = new AssetQueryParameters(null, null, "quantity", "asc");
            var assets = CreateSampleAssetList();
            var sortedAssets = assets.OrderBy(a => a.Quantity).ToList();
            var assetDtos = sortedAssets.Select(a => CreateSampleAssetDto(a.AssetId, a.BuildingId, a.Info!, a.Quantity)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(assets);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(assetDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Data.First().Quantity.Should().Be(5);
        }

        [Fact]
        public async Task GetAllAsync_WithSortByQuantity_Descending_ShouldReturnSortedAssets()
        {
            // Arrange
            var query = new AssetQueryParameters(null, null, "quantity", "desc");
            var assets = CreateSampleAssetList();
            var sortedAssets = assets.OrderByDescending(a => a.Quantity).ToList();
            var assetDtos = sortedAssets.Select(a => CreateSampleAssetDto(a.AssetId, a.BuildingId, a.Info!, a.Quantity)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(assets);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(assetDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Data.First().Quantity.Should().Be(15);
        }

        [Fact]
        public async Task GetAllAsync_WithNoSortBy_ShouldSortByCreatedAtDescending()
        {
            // Arrange
            var query = new AssetQueryParameters(null, null, null, null);
            var assets = CreateSampleAssetList();
            
            // Set different CreatedAt dates for testing
            assets[0].CreatedAt = DateTime.UtcNow.AddDays(-10);
            assets[1].CreatedAt = DateTime.UtcNow.AddDays(-5);
            assets[2].CreatedAt = DateTime.UtcNow.AddDays(-2);
            assets[3].CreatedAt = DateTime.UtcNow.AddDays(-1);

            var sortedAssets = assets.OrderByDescending(a => a.CreatedAt).ToList();
            var assetDtos = sortedAssets.Select(a => CreateSampleAssetDto(a.AssetId, a.BuildingId, a.Info!, a.Quantity)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(assets);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(assetDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
        }

        [Fact]
        public async Task GetAllAsync_WhenNoAssetsFound_ShouldReturnEmptyListWithNoResultsMessage()
        {
            // Arrange
            var query = new AssetQueryParameters("non-existent-building", null, null, null);
            var emptyList = new List<Asset>();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(emptyList);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(new List<AssetDto>());

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
            var query = new AssetQueryParameters(null, null, null, null);

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync((List<Asset>)null!);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(new List<AssetDto>());

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            result.Message.Should().Be(ApiResponse.SM01_NO_RESULTS);
        }

        [Fact]
        public async Task GetAllAsync_WithCombinedFilters_ShouldReturnFilteredAndSortedAssets()
        {
            // Arrange
            var query = new AssetQueryParameters("building-001", "bàn", "quantity", "desc");
            var assets = CreateSampleAssetList()
                .Where(a => a.BuildingId == "building-001" && a.Info!.ToLower().Contains("bàn"))
                .OrderByDescending(a => a.Quantity)
                .ToList();
            var assetDtos = assets.Select(a => CreateSampleAssetDto(a.AssetId, a.BuildingId, a.Info!, a.Quantity)).ToList();

            _repositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(assets);
            _mapperMock.Setup(m => m.Map<IEnumerable<AssetDto>>(It.IsAny<IEnumerable<Asset>>()))
                .Returns(assetDtos);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().OnlyContain(a => a.BuildingId == "building-001" && a.Info!.ToLower().Contains("bàn"));
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WhenAssetExists_ShouldReturnAssetDto()
        {
            // Arrange
            var assetId = "asset-001";
            var asset = CreateSampleAsset(assetId);
            var assetDto = CreateSampleAssetDto(assetId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(asset);
            _mapperMock.Setup(m => m.Map<AssetDto?>(asset))
                .Returns(assetDto);

            // Act
            var result = await _service.GetByIdAsync(assetId);

            // Assert
            result.Should().NotBeNull();
            result!.AssetId.Should().Be(assetId);
            _repositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenAssetNotFound_ShouldReturnNull()
        {
            // Arrange
            var assetId = "non-existent-asset";

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync((Asset)null!);
            _mapperMock.Setup(m => m.Map<AssetDto?>(null))
                .Returns((AssetDto)null!);

            // Act
            var result = await _service.GetByIdAsync(assetId);

            // Assert
            result.Should().BeNull();
            _repositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()), Times.Once);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidDto_ShouldCreateAsset()
        {
            // Arrange
            var createDto = CreateSampleAssetCreateDto();
            var asset = new Asset
            {
                BuildingId = createDto.BuildingId,
                Info = createDto.Info,
                Quantity = createDto.Quantity
            };
            var assetDto = CreateSampleAssetDto();

            _mapperMock.Setup(m => m.Map<Asset>(createDto))
                .Returns(asset);
            _mapperMock.Setup(m => m.Map<AssetDto>(It.IsAny<Asset>()))
                .Returns(assetDto);
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Asset>()))
                .ReturnsAsync((Asset a) => a);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.AssetId.Should().NotBeNullOrEmpty();
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Asset>(a => 
                !string.IsNullOrEmpty(a.AssetId) &&
                a.CreatedAt.HasValue &&
                a.UpdatedAt.HasValue
            )), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldSetCreatedAtAndUpdatedAt()
        {
            // Arrange
            var createDto = CreateSampleAssetCreateDto();
            var asset = new Asset
            {
                BuildingId = createDto.BuildingId,
                Info = createDto.Info,
                Quantity = createDto.Quantity
            };
            var assetDto = CreateSampleAssetDto();

            _mapperMock.Setup(m => m.Map<Asset>(createDto))
                .Returns(asset);
            _mapperMock.Setup(m => m.Map<AssetDto>(It.IsAny<Asset>()))
                .Returns(assetDto);
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Asset>()))
                .ReturnsAsync((Asset a) => a);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Asset>(a =>
                a.CreatedAt.HasValue &&
                a.UpdatedAt.HasValue &&
                (a.UpdatedAt.Value - a.CreatedAt.Value).TotalSeconds < 1
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldGenerateUniqueAssetId()
        {
            // Arrange
            var createDto = CreateSampleAssetCreateDto();
            var asset = new Asset
            {
                BuildingId = createDto.BuildingId,
                Info = createDto.Info,
                Quantity = createDto.Quantity
            };
            var assetDto = CreateSampleAssetDto();

            _mapperMock.Setup(m => m.Map<Asset>(createDto))
                .Returns(asset);
            _mapperMock.Setup(m => m.Map<AssetDto>(It.IsAny<Asset>()))
                .Returns(assetDto);
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Asset>()))
                .ReturnsAsync((Asset a) => a);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Asset>(a =>
                !string.IsNullOrEmpty(a.AssetId) &&
                a.AssetId.Length == 32 // GUID with "N" format
            )), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WhenAssetExists_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var assetId = "asset-001";
            var updateDto = CreateSampleAssetUpdateDto();
            var existingAsset = CreateSampleAsset(assetId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(existingAsset);
            _mapperMock.Setup(m => m.Map(updateDto, existingAsset))
                .Returns(existingAsset);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Asset>()))
                .ReturnsAsync((Asset a) => a);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(assetId, updateDto);

            // Assert
            result.Should().BeTrue();
            _repositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(existingAsset), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenAssetExists_ShouldUpdateUpdatedAt()
        {
            // Arrange
            var assetId = "asset-001";
            var updateDto = CreateSampleAssetUpdateDto();
            var existingAsset = CreateSampleAsset(assetId);
            var oldUpdatedAt = existingAsset.UpdatedAt;

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(existingAsset);
            _mapperMock.Setup(m => m.Map(updateDto, existingAsset))
                .Returns(existingAsset);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Asset>()))
                .ReturnsAsync((Asset a) => a);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _service.UpdateAsync(assetId, updateDto);

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Asset>(a =>
                a.UpdatedAt > oldUpdatedAt &&
                a.UpdatedAt.Value <= DateTime.UtcNow
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenAssetNotFound_ShouldReturnFalse()
        {
            // Arrange
            var assetId = "non-existent-asset";
            var updateDto = CreateSampleAssetUpdateDto();

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync((Asset)null!);

            // Act
            var result = await _service.UpdateAsync(assetId, updateDto);

            // Assert
            result.Should().BeFalse();
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Asset>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var assetId = "asset-001";
            var updateDto = CreateSampleAssetUpdateDto();
            var existingAsset = CreateSampleAsset(assetId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(existingAsset);
            _mapperMock.Setup(m => m.Map(updateDto, existingAsset))
                .Returns(existingAsset);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Asset>()))
                .ReturnsAsync((Asset a) => a);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateAsync(assetId, updateDto);

            // Assert
            result.Should().BeFalse();
            _repositoryMock.Verify(r => r.UpdateAsync(existingAsset), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WhenAssetExists_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            var assetId = "asset-001";
            var existingAsset = CreateSampleAsset(assetId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(existingAsset);
            _repositoryMock.Setup(r => r.RemoveAsync(It.IsAny<Asset>()))
                .Returns(Task.CompletedTask);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(assetId);

            // Assert
            result.Should().BeTrue();
            _repositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()), Times.Once);
            _repositoryMock.Verify(r => r.RemoveAsync(existingAsset), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenAssetNotFound_ShouldReturnFalse()
        {
            // Arrange
            var assetId = "non-existent-asset";

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync((Asset)null!);

            // Act
            var result = await _service.DeleteAsync(assetId);

            // Assert
            result.Should().BeFalse();
            _repositoryMock.Verify(r => r.RemoveAsync(It.IsAny<Asset>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var assetId = "asset-001";
            var existingAsset = CreateSampleAsset(assetId);

            _repositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Asset, bool>>>()))
                .ReturnsAsync(existingAsset);
            _repositoryMock.Setup(r => r.RemoveAsync(It.IsAny<Asset>()))
                .Returns(Task.CompletedTask);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAsync(assetId);

            // Assert
            result.Should().BeFalse();
            _repositoryMock.Verify(r => r.RemoveAsync(existingAsset), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}

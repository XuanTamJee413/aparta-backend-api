using Xunit;
using Moq;
using FluentAssertions;
using ApartaAPI.Services;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Models;
using ApartaAPI.DTOs.Contracts;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Task = System.Threading.Tasks.Task;
using Microsoft.AspNetCore.Http;

namespace ServiceUnitTest.TestContract
{
    /// <summary>
    /// Test suite cho ContractService - Bao gồm các test case đầy đủ và dễ tái sử dụng
    /// </summary>
    public class ContractServiceTests
    {
        private readonly Mock<IRepository<Contract>> _contractRepositoryMock;
        private readonly Mock<IRepository<Apartment>> _apartmentRepositoryMock;
        private readonly Mock<IRepository<ApartmentMember>> _apartmentMemberRepositoryMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ContractService _service;

        public ContractServiceTests()
        {
            _contractRepositoryMock = new Mock<IRepository<Contract>>();
            _apartmentRepositoryMock = new Mock<IRepository<Apartment>>();
            _apartmentMemberRepositoryMock = new Mock<IRepository<ApartmentMember>>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            _cloudinaryServiceMock = new Mock<ICloudinaryService>();
            _mapperMock = new Mock<IMapper>();
            
            _service = new ContractService(
                _contractRepositoryMock.Object,
                _apartmentRepositoryMock.Object,
                _apartmentMemberRepositoryMock.Object,
                _userRepositoryMock.Object,
                _cloudinaryServiceMock.Object,
                _mapperMock.Object
            );
        }

        #region Helper Methods - Dễ dàng tái sử dụng

        /// <summary>
        /// Tạo Contract mẫu để test
        /// </summary>
        private Contract CreateSampleContract(
            string contractId = "contract-001",
            string apartmentId = "apt-001",
            string? image = "https://example.com/contract.jpg",
            DateOnly? startDate = null,
            DateOnly? endDate = null)
        {
            return new Contract
            {
                ContractId = contractId,
                ApartmentId = apartmentId,
                Image = image,
                StartDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                EndDate = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(335)),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
        }

        /// <summary>
        /// Tạo ContractDto mẫu để test
        /// </summary>
        private ContractDto CreateSampleContractDto(
            string contractId = "contract-001",
            string apartmentId = "apt-001",
            string? apartmentCode = "A101",
            string? ownerName = "Nguyễn Văn A",
            string? ownerPhone = "0123456789",
            string? ownerEmail = "nguyenvana@email.com")
        {
            return new ContractDto
            {
                ContractId = contractId,
                ApartmentId = apartmentId,
                ApartmentCode = apartmentCode,
                OwnerName = ownerName,
                OwnerPhoneNumber = ownerPhone,
                OwnerEmail = ownerEmail,
                Image = "https://example.com/contract.jpg",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(335)),
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };
        }

        /// <summary>
        /// Tạo ContractCreateDto mẫu để test
        /// </summary>
        private ContractCreateDto CreateSampleContractCreateDto(
            string apartmentId = "apt-001",
            string ownerName = "Nguyễn Văn A",
            string ownerPhone = "0123456789",
            string ownerEmail = "nguyenvana@email.com",
            string ownerIdNumber = "123456789012")
        {
            return new ContractCreateDto(
                ApartmentId: apartmentId,
                Image: "https://example.com/contract.jpg",
                StartDate: DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                OwnerName: ownerName,
                OwnerPhoneNumber: ownerPhone,
                OwnerIdNumber: ownerIdNumber,
                OwnerEmail: ownerEmail,
                OwnerGender: "Nam",
                OwnerDateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                OwnerNationality: "Việt Nam"
            );
        }

        /// <summary>
        /// Tạo ContractUpdateDto mẫu để test
        /// </summary>
        private ContractUpdateDto CreateSampleContractUpdateDto(
            DateOnly? endDate = null,
            string? image = "https://example.com/updated-contract.jpg")
        {
            return new ContractUpdateDto
            {
                EndDate = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                Image = image
            };
        }

        /// <summary>
        /// Tạo Apartment mẫu để test
        /// </summary>
        private Apartment CreateSampleApartment(
            string apartmentId = "apt-001",
            string code = "A101",
            string status = "Còn Trống")
        {
            return new Apartment
            {
                ApartmentId = apartmentId,
                BuildingId = "building-001",
                Code = code,
                Type = "1PN",
                Status = status,
                Area = 50.5,
                Floor = 5,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
        }

        /// <summary>
        /// Tạo ApartmentMember mẫu để test
        /// </summary>
        private ApartmentMember CreateSampleApartmentMember(
            string memberId = "member-001",
            string apartmentId = "apt-001",
            string name = "Nguyễn Văn A",
            bool isOwner = true,
            string status = "Đang cư trú",
            string phoneNumber = "0123456789",
            string idNumber = "123456789012")
        {
            return new ApartmentMember
            {
                ApartmentMemberId = memberId,
                ApartmentId = apartmentId,
                Name = name,
                PhoneNumber = phoneNumber,
                IdNumber = idNumber,
                Gender = "Nam",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                Nationality = "Việt Nam",
                IsOwner = isOwner,
                FamilyRole = "Chủ Hộ",
                Status = status,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
        }

        /// <summary>
        /// Tạo User mẫu để test
        /// </summary>
        private User CreateSampleUser(
            string userId = "user-001",
            string apartmentId = "apt-001",
            string email = "nguyenvana@email.com",
            string name = "Nguyễn Văn A")
        {
            return new User
            {
                UserId = userId,
                RoleId = "EC13BABB-416F-42EB-BFD4-0725493A63D0",
                ApartmentId = apartmentId,
                Email = email,
                Phone = "0123456789",
                Name = name,
                PasswordHash = "$2a$12$s7OmJwjZnyB8qCrL9KifvORA461N/6WgzDfvAyRUMhWVVkHuPecZ.",
                Status = "Active",
                IsDeleted = false,
                IsFirstLogin = false,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
        }

        /// <summary>
        /// Tạo danh sách Contracts mẫu để test
        /// </summary>
        private List<Contract> CreateSampleContractList()
        {
            var now = DateTime.UtcNow;
            return new List<Contract>
            {
                CreateSampleContract("contract-001", "apt-001", startDate: DateOnly.FromDateTime(now.AddDays(-60)), endDate: DateOnly.FromDateTime(now.AddDays(305))),
                CreateSampleContract("contract-002", "apt-002", startDate: DateOnly.FromDateTime(now.AddDays(-30)), endDate: DateOnly.FromDateTime(now.AddDays(335))),
                CreateSampleContract("contract-003", "apt-001", startDate: DateOnly.FromDateTime(now.AddDays(-90)), endDate: DateOnly.FromDateTime(now.AddDays(275))),
                CreateSampleContract("contract-004", "apt-003", startDate: DateOnly.FromDateTime(now.AddDays(-120)), endDate: DateOnly.FromDateTime(now.AddDays(245)))
            };
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_WithNullQuery_ShouldReturnAllContracts()
        {
            // Arrange
            var contracts = CreateSampleContractList();
            var apartments = new List<Apartment>
            {
                CreateSampleApartment("apt-001", "A101"),
                CreateSampleApartment("apt-002", "A102"),
                CreateSampleApartment("apt-003", "A103")
            };
            var members = new List<ApartmentMember>
            {
                CreateSampleApartmentMember("member-001", "apt-001", "Owner 1"),
                CreateSampleApartmentMember("member-002", "apt-002", "Owner 2"),
                CreateSampleApartmentMember("member-003", "apt-003", "Owner 3")
            };
            var users = new List<User>
            {
                CreateSampleUser("user-001", "apt-001", "owner1@email.com", "Owner 1"),
                CreateSampleUser("user-002", "apt-002", "owner2@email.com", "Owner 2"),
                CreateSampleUser("user-003", "apt-003", "owner3@email.com", "Owner 3")
            };

            _contractRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(contracts);
            _apartmentRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartments);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(members);
            _userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync(null!);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Message.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_WithApartmentIdFilter_ShouldReturnFilteredContracts()
        {
            // Arrange
            var query = new ContractQueryParameters("apt-001", null, null);
            var contracts = CreateSampleContractList().Where(c => c.ApartmentId == "apt-001").ToList();
            var apartments = new List<Apartment> { CreateSampleApartment("apt-001", "A101") };
            var members = new List<ApartmentMember> { CreateSampleApartmentMember("member-001", "apt-001") };
            var users = new List<User> { CreateSampleUser("user-001", "apt-001") };

            _contractRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(contracts);
            _apartmentRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartments);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(members);
            _userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(c => c.ApartmentId == "apt-001");
        }

        [Fact]
        public async Task GetAllAsync_WithSortByStartDate_Ascending_ShouldReturnSortedContracts()
        {
            // Arrange
            var query = new ContractQueryParameters(null, "startdate", "asc");
            var contracts = CreateSampleContractList();
            var apartments = new List<Apartment>
            {
                CreateSampleApartment("apt-001", "A101"),
                CreateSampleApartment("apt-002", "A102"),
                CreateSampleApartment("apt-003", "A103")
            };
            var members = new List<ApartmentMember>
            {
                CreateSampleApartmentMember("member-001", "apt-001"),
                CreateSampleApartmentMember("member-002", "apt-002"),
                CreateSampleApartmentMember("member-003", "apt-003")
            };
            var users = new List<User>
            {
                CreateSampleUser("user-001", "apt-001"),
                CreateSampleUser("user-002", "apt-002"),
                CreateSampleUser("user-003", "apt-003")
            };

            _contractRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(contracts);
            _apartmentRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartments);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(members);
            _userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Data.First().ContractId.Should().Be("contract-004"); // Oldest start date
        }

        [Fact]
        public async Task GetAllAsync_WithSortByEndDate_Descending_ShouldReturnSortedContracts()
        {
            // Arrange
            var query = new ContractQueryParameters(null, "enddate", "desc");
            var contracts = CreateSampleContractList();
            var apartments = new List<Apartment>
            {
                CreateSampleApartment("apt-001", "A101"),
                CreateSampleApartment("apt-002", "A102"),
                CreateSampleApartment("apt-003", "A103")
            };
            var members = new List<ApartmentMember>
            {
                CreateSampleApartmentMember("member-001", "apt-001"),
                CreateSampleApartmentMember("member-002", "apt-002"),
                CreateSampleApartmentMember("member-003", "apt-003")
            };
            var users = new List<User>
            {
                CreateSampleUser("user-001", "apt-001"),
                CreateSampleUser("user-002", "apt-002"),
                CreateSampleUser("user-003", "apt-003")
            };

            _contractRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(contracts);
            _apartmentRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartments);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(members);
            _userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().HaveCount(4);
            result.Data.First().ContractId.Should().Be("contract-002"); // Latest end date
        }

        [Fact]
        public async Task GetAllAsync_WhenNoContractsFound_ShouldReturnEmptyListWithNoResultsMessage()
        {
            // Arrange
            var query = new ContractQueryParameters("non-existent-apartment", null, null);
            var emptyList = new List<Contract>();

            _contractRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(emptyList);

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
            var query = new ContractQueryParameters(null, null, null);

            _contractRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync((List<Contract>)null!);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEmpty();
            result.Message.Should().Be(ApiResponse.SM01_NO_RESULTS);
        }

        [Fact]
        public async Task GetAllAsync_ShouldIncludeOwnerInformation()
        {
            // Arrange
            var query = new ContractQueryParameters(null, null, null);
            var contracts = new List<Contract> { CreateSampleContract("contract-001", "apt-001") };
            var apartments = new List<Apartment> { CreateSampleApartment("apt-001", "A101") };
            var members = new List<ApartmentMember> { CreateSampleApartmentMember("member-001", "apt-001", "Nguyễn Văn A") };
            var users = new List<User> { CreateSampleUser("user-001", "apt-001", "owner@email.com", "Nguyễn Văn A") };

            _contractRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(contracts);
            _apartmentRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartments);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(members);
            _userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync(query);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.First().OwnerName.Should().Be("Nguyễn Văn A");
            result.Data.First().OwnerPhoneNumber.Should().Be("0123456789");
            result.Data.First().OwnerEmail.Should().Be("owner@email.com");
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WhenContractExists_ShouldReturnContractDto()
        {
            // Arrange
            var contractId = "contract-001";
            var contract = CreateSampleContract(contractId, "apt-001");
            var apartment = CreateSampleApartment("apt-001", "A101");
            var member = CreateSampleApartmentMember("member-001", "apt-001", "Nguyễn Văn A", true, "Đang cư trú");
            var user = CreateSampleUser("user-001", "apt-001", "owner@email.com", "Nguyễn Văn A");

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(contract);
            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(new List<ApartmentMember> { member });
            _userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new List<User> { user });

            // Act
            var result = await _service.GetByIdAsync(contractId);

            // Assert
            result.Should().NotBeNull();
            result!.ContractId.Should().Be(contractId);
            result.ApartmentId.Should().Be("apt-001");
            result.ApartmentCode.Should().Be("A101");
            result.OwnerName.Should().Be("Nguyễn Văn A");
            _contractRepositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenContractNotFound_ShouldReturnNull()
        {
            // Arrange
            var contractId = "non-existent-contract";

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync((Contract)null!);

            // Act
            var result = await _service.GetByIdAsync(contractId);

            // Assert
            result.Should().BeNull();
            _contractRepositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNoOwnerMember_ShouldUseUserInfo()
        {
            // Arrange
            var contractId = "contract-001";
            var contract = CreateSampleContract(contractId, "apt-001");
            var apartment = CreateSampleApartment("apt-001", "A101");
            var user = CreateSampleUser("user-001", "apt-001", "user@email.com", "User Name");

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(contract);
            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(new List<ApartmentMember>()); // No owner members
            _userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new List<User> { user });

            // Act
            var result = await _service.GetByIdAsync(contractId);

            // Assert
            result.Should().NotBeNull();
            result!.OwnerName.Should().Be("User Name");
            result.OwnerEmail.Should().Be("user@email.com");
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidDto_ShouldCreateContract()
        {
            // Arrange
            var createDto = CreateSampleContractCreateDto();
            var apartment = CreateSampleApartment("apt-001", "A101", "Còn Trống");
            var contract = CreateSampleContract("new-contract", createDto.ApartmentId);

            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync((ApartmentMember)null!); // No duplicate
            _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null!); // No existing user/email
            _mapperMock.Setup(m => m.Map<Contract>(createDto))
                .Returns(contract);
            _mapperMock.Setup(m => m.Map<ContractDto>(It.IsAny<Contract>()))
                .Returns(CreateSampleContractDto());
            _contractRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Contract>()))
                .ReturnsAsync((Contract c) => c);
            _apartmentMemberRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ApartmentMember>()))
                .ReturnsAsync((ApartmentMember m) => m);
            _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            _contractRepositoryMock.Verify(r => r.AddAsync(It.Is<Contract>(c =>
                !string.IsNullOrEmpty(c.ContractId) &&
                c.CreatedAt.HasValue &&
                c.UpdatedAt.HasValue
            )), Times.Once);
            _apartmentMemberRepositoryMock.Verify(r => r.AddAsync(It.Is<ApartmentMember>(m =>
                m.IsOwner == true &&
                m.Status == "Đang cư trú" &&
                m.FamilyRole == "Chủ Hộ"
            )), Times.Once);
            _contractRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenApartmentNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var createDto = CreateSampleContractCreateDto();

            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync((Apartment)null!);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(createDto));
            _contractRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenApartmentNotAvailable_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = CreateSampleContractCreateDto();
            var apartment = CreateSampleApartment("apt-001", "A101", "Đã Bán"); // Already sold

            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
            _contractRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateIdNumber_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = CreateSampleContractCreateDto();
            var apartment = CreateSampleApartment("apt-001", "A101", "Chưa Thuê");
            var existingMember = CreateSampleApartmentMember(idNumber: createDto.OwnerIdNumber);

            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.Is<Expression<Func<ApartmentMember, bool>>>(
                expr => expr.ToString().Contains("IdNumber"))))
                .ReturnsAsync(existingMember); // Duplicate IdNumber

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
            _contractRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicatePhoneNumber_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = CreateSampleContractCreateDto();
            var apartment = CreateSampleApartment("apt-001", "A101", "Chưa Thuê");
            var existingMember = CreateSampleApartmentMember(phoneNumber: createDto.OwnerPhoneNumber);

            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.Is<Expression<Func<ApartmentMember, bool>>>(
                expr => expr.ToString().Contains("IdNumber"))))
                .ReturnsAsync((ApartmentMember)null!); // No duplicate IdNumber
            _apartmentMemberRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.Is<Expression<Func<ApartmentMember, bool>>>(
                expr => expr.ToString().Contains("PhoneNumber"))))
                .ReturnsAsync(existingMember); // Duplicate PhoneNumber

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
            _contractRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = CreateSampleContractCreateDto();
            var apartment = CreateSampleApartment("apt-001", "A101", "Chưa Thuê");
            var existingUser = CreateSampleUser(email: createDto.OwnerEmail);

            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync((ApartmentMember)null!); // No duplicate member info
            _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(existingUser); // Duplicate email

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
            _contractRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldUpdateApartmentStatusToDaBan()
        {
            // Arrange
            var createDto = CreateSampleContractCreateDto();
            var apartment = CreateSampleApartment("apt-001", "A101", "Còn Trống");
            var contract = CreateSampleContract("new-contract", createDto.ApartmentId);

            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync((ApartmentMember)null!);
            _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null!);
            _mapperMock.Setup(m => m.Map<Contract>(createDto))
                .Returns(contract);
            _mapperMock.Setup(m => m.Map<ContractDto>(It.IsAny<Contract>()))
                .Returns(CreateSampleContractDto());
            _contractRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Contract>()))
                .ReturnsAsync((Contract c) => c);
            _apartmentMemberRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ApartmentMember>()))
                .ReturnsAsync((ApartmentMember m) => m);
            _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _service.CreateAsync(createDto);

            // Assert
            apartment.Status.Should().Be("Đã Bán");
            apartment.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldGenerateUniqueContractId()
        {
            // Arrange
            var createDto = CreateSampleContractCreateDto();
            var apartment = CreateSampleApartment("apt-001", "A101", "Còn Trống");
            var contract = new Contract { ApartmentId = createDto.ApartmentId };

            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync((ApartmentMember)null!);
            _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null!);
            _mapperMock.Setup(m => m.Map<Contract>(createDto))
                .Returns(contract);
            _mapperMock.Setup(m => m.Map<ContractDto>(It.IsAny<Contract>()))
                .Returns(CreateSampleContractDto());
            _contractRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Contract>()))
                .ReturnsAsync((Contract c) => c);
            _apartmentMemberRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ApartmentMember>()))
                .ReturnsAsync((ApartmentMember m) => m);
            _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _service.CreateAsync(createDto);

            // Assert
            _contractRepositoryMock.Verify(r => r.AddAsync(It.Is<Contract>(c =>
                !string.IsNullOrEmpty(c.ContractId) &&
                c.ContractId.Length == 32 // GUID with "N" format
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenUserExists_ShouldUpdateExistingUser()
        {
            // Arrange
            var createDto = CreateSampleContractCreateDto();
            var apartment = CreateSampleApartment("apt-001", "A101", "Còn Trống");
            var contract = CreateSampleContract("new-contract", createDto.ApartmentId);
            var existingUser = CreateSampleUser("existing-user", createDto.ApartmentId, "old@email.com", "Old Name");

            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync((ApartmentMember)null!);
            _userRepositoryMock.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null!) // For email check
                .ReturnsAsync(existingUser); // For existing user check
            _mapperMock.Setup(m => m.Map<Contract>(createDto))
                .Returns(contract);
            _mapperMock.Setup(m => m.Map<ContractDto>(It.IsAny<Contract>()))
                .Returns(CreateSampleContractDto());
            _contractRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Contract>()))
                .ReturnsAsync((Contract c) => c);
            _apartmentMemberRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ApartmentMember>()))
                .ReturnsAsync((ApartmentMember m) => m);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _service.CreateAsync(createDto);

            // Assert
            existingUser.Name.Should().Be(createDto.OwnerName);
            existingUser.Email.Should().Be(createDto.OwnerEmail);
            existingUser.Phone.Should().Be(createDto.OwnerPhoneNumber);
            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WhenContractExists_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var contractId = "contract-001";
            var updateDto = CreateSampleContractUpdateDto();
            var existingContract = CreateSampleContract(contractId);

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);
            _contractRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contract>()))
                .ReturnsAsync((Contract c) => c);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(contractId, updateDto);

            // Assert
            result.Should().BeTrue();
            _contractRepositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()), Times.Once);
            _contractRepositoryMock.Verify(r => r.UpdateAsync(existingContract), Times.Once);
            _contractRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenContractExists_ShouldUpdateUpdatedAt()
        {
            // Arrange
            var contractId = "contract-001";
            var updateDto = CreateSampleContractUpdateDto();
            var existingContract = CreateSampleContract(contractId);
            var oldUpdatedAt = existingContract.UpdatedAt;

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);
            _contractRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contract>()))
                .ReturnsAsync((Contract c) => c);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _service.UpdateAsync(contractId, updateDto);

            // Assert
            _contractRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Contract>(c =>
                c.UpdatedAt > oldUpdatedAt &&
                c.UpdatedAt.Value <= DateTime.UtcNow
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenEndDateIsBeforeStartDate_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var contractId = "contract-001";
            var existingContract = CreateSampleContract(contractId, startDate: DateOnly.FromDateTime(DateTime.UtcNow));
            var updateDto = new ContractUpdateDto
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)) // Before start date
            };

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(contractId, updateDto));
            _contractRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contract>()), Times.Never);
            _contractRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenContractNotFound_ShouldReturnFalse()
        {
            // Arrange
            var contractId = "non-existent-contract";
            var updateDto = CreateSampleContractUpdateDto();

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync((Contract)null!);

            // Act
            var result = await _service.UpdateAsync(contractId, updateDto);

            // Assert
            result.Should().BeFalse();
            _contractRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contract>()), Times.Never);
            _contractRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var contractId = "contract-001";
            var updateDto = CreateSampleContractUpdateDto();
            var existingContract = CreateSampleContract(contractId);

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);
            _contractRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contract>()))
                .ReturnsAsync((Contract c) => c);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateAsync(contractId, updateDto);

            // Assert
            result.Should().BeFalse();
            _contractRepositoryMock.Verify(r => r.UpdateAsync(existingContract), Times.Once);
            _contractRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithImageFile_ShouldUploadToCloudinary()
        {
            // Arrange
            var contractId = "contract-001";
            var existingContract = CreateSampleContract(contractId);
            var mockFormFile = new Mock<IFormFile>();
            mockFormFile.Setup(f => f.Length).Returns(1024);
            
            var updateDto = new ContractUpdateDto
            {
                ImageFile = mockFormFile.Object,
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2))
            };

            var cloudinaryResult = new CloudinaryUploadResultDto
            {
                SecureUrl = "https://cloudinary.com/new-image.jpg",
                PublicId = "test-public-id",
                ResourceType = "image",
                Format = "jpg",
                Bytes = 1024
            };

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);
            _cloudinaryServiceMock.Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cloudinaryResult);
            _contractRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contract>()))
                .ReturnsAsync((Contract c) => c);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(contractId, updateDto);

            // Assert
            result.Should().BeTrue();
            existingContract.Image.Should().Be("https://cloudinary.com/new-image.jpg");
            _cloudinaryServiceMock.Verify(s => s.UploadImageAsync(mockFormFile.Object, "contracts", It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task UpdateAsync_WithImageString_ShouldUpdateImageUrl()
        {
            // Arrange
            var contractId = "contract-001";
            var existingContract = CreateSampleContract(contractId);
            var newImageUrl = "https://example.com/new-contract.jpg";
            
            var updateDto = new ContractUpdateDto
            {
                Image = newImageUrl,
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2))
            };

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);
            _contractRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contract>()))
                .ReturnsAsync((Contract c) => c);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(contractId, updateDto);

            // Assert
            result.Should().BeTrue();
            existingContract.Image.Should().Be(newImageUrl);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WhenContractExpired_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            var contractId = "contract-001";
            var expiredEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)); // Expired
            var existingContract = CreateSampleContract(contractId, "apt-001", endDate: expiredEndDate);
            var apartment = CreateSampleApartment("apt-001", "A101", "Đã Bán");
            var ownerMembers = new List<ApartmentMember>
            {
                CreateSampleApartmentMember("member-001", "apt-001", "Owner", true, "Đang cư trú")
            };

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);
            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(ownerMembers);
            _apartmentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Apartment>()))
                .ReturnsAsync((Apartment a) => a);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(contractId);

            // Assert
            result.Should().BeTrue();
            _contractRepositoryMock.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()), Times.Once);
            _contractRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenContractNotExpired_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var contractId = "contract-001";
            var futureEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)); // Not expired
            var existingContract = CreateSampleContract(contractId, endDate: futureEndDate);

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(contractId));
            _contractRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenContractNotFound_ShouldReturnFalse()
        {
            // Arrange
            var contractId = "non-existent-contract";

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync((Contract)null!);

            // Act
            var result = await _service.DeleteAsync(contractId);

            // Assert
            result.Should().BeFalse();
            _contractRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldCloseOldApartmentAndCreateNewOne()
        {
            // Arrange
            var contractId = "contract-001";
            var expiredEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
            var existingContract = CreateSampleContract(contractId, "apt-001", endDate: expiredEndDate);
            var apartment = CreateSampleApartment("apt-001", "A101", "Đã Bán");
            var ownerMembers = new List<ApartmentMember>
            {
                CreateSampleApartmentMember("member-001", "apt-001", "Owner", true, "Đang cư trú")
            };

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);
            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(ownerMembers);
            _apartmentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Apartment>()))
                .ReturnsAsync((Apartment a) => a);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(contractId);

            // Assert
            result.Should().BeTrue();
            apartment.Status.Should().Be("Đã Đóng");
            apartment.Code.Should().StartWith("A101-HIS-");
            
            _apartmentRepositoryMock.Verify(r => r.AddAsync(It.Is<Apartment>(a =>
                a.Code == "A101" &&
                a.Status == "Còn Trống" &&
                !string.IsNullOrEmpty(a.ApartmentId) &&
                a.BuildingId == apartment.BuildingId
            )), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldUpdateOwnerMemberStatus()
        {
            // Arrange
            var contractId = "contract-001";
            var expiredEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
            var existingContract = CreateSampleContract(contractId, "apt-001", endDate: expiredEndDate);
            var apartment = CreateSampleApartment("apt-001", "A101", "Đã Bán");
            var ownerMember = CreateSampleApartmentMember("member-001", "apt-001", "Owner", true, "Đang cư trú");
            var ownerMembers = new List<ApartmentMember> { ownerMember };

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);
            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);
            _apartmentMemberRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ApartmentMember, bool>>>()))
                .ReturnsAsync(ownerMembers);
            _apartmentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Apartment>()))
                .ReturnsAsync((Apartment a) => a);
            _contractRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(contractId);

            // Assert
            result.Should().BeTrue();
            ownerMember.Status.Should().Be("Đã rời đi");
            ownerMember.IsOwner.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_WhenApartmentAlreadyClosed_ShouldReturnFalse()
        {
            // Arrange
            var contractId = "contract-001";
            var expiredEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
            var existingContract = CreateSampleContract(contractId, "apt-001", endDate: expiredEndDate);
            var apartment = CreateSampleApartment("apt-001", "A101-HIS-20231120", "Đã Đóng");

            _contractRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(existingContract);
            _apartmentRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Apartment, bool>>>()))
                .ReturnsAsync(apartment);

            // Act
            var result = await _service.DeleteAsync(contractId);

            // Assert
            result.Should().BeFalse();
            _contractRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        #endregion
    }
}

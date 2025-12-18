using ApartaAPI.Data;
using ApartaAPI.DTOs.Proposals;
using ApartaAPI.Models;
using ApartaAPI.Profiles;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceUnitTest.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Services
{
    public class ProposalServiceTests
    {
        private readonly Mock<IProposalRepository> _mockProposalRepo;
        private readonly Mock<IRepository<User>> _mockUserRepo;
        private readonly Mock<IRepository<StaffBuildingAssignment>> _mockAssignmentRepo;
        private readonly IMapper _mapper;
        private readonly ApartaDbContext _context;
        private readonly ProposalService _service;

        public ProposalServiceTests()
        {
            _mockProposalRepo = new Mock<IProposalRepository>();
            _mockUserRepo = new Mock<IRepository<User>>();
            _mockAssignmentRepo = new Mock<IRepository<StaffBuildingAssignment>>();

            // Real Mapper Setup
            var config = new MapperConfiguration(cfg => {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = config.CreateMapper();

            // InMemory DbContext Setup
            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);

            _service = new ProposalService(
                _mockProposalRepo.Object,
                _mockUserRepo.Object,
                _mockAssignmentRepo.Object,
                _mapper,
                _context
            );
        }

        [Fact]
        public async Task CreateProposalAsync_WithValidData_ReturnsProposalDto()
        {
            // Arrange
            var residentId = "u1";
            var createDto = new ProposalCreateDto { Content = "Test Content" };
            var proposal = new Proposal { ProposalId = "p1", ResidentId = residentId, Content = "Test Content", Status = "Pending", CreatedAt = DateTime.UtcNow };

            _mockProposalRepo.Setup(r => r.AddAsync(It.IsAny<Proposal>())).ReturnsAsync(proposal);
            _mockProposalRepo.Setup(r => r.GetProposalDetailsAsync("p1")).ReturnsAsync(proposal);

            // Act
            var result = await _service.CreateProposalAsync(residentId, createDto);

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("Test Content");
            result.Status.Should().Be("Pending");
            _mockProposalRepo.Verify(r => r.AddAsync(It.IsAny<Proposal>()), Times.Once);
        }

        [Fact]
        public async Task GetProposalsByResidentAsync_WithValidResident_ReturnsList()
        {
            // Arrange
            var residentId = "u1";
            var proposals = new List<Proposal> 
            { 
                new Proposal { ProposalId = "p1", ResidentId = residentId, Resident = new User { Name = "Res", PasswordHash = "hash" }, Content = "Content" } 
            };
            _mockProposalRepo.Setup(r => r.GetResidentProposalsAsync(residentId)).ReturnsAsync(proposals);

            // Act
            var result = await _service.GetProposalsByResidentAsync(residentId);

            // Assert
            result.Should().HaveCount(1);
            result.First().ProposalId.Should().Be("p1");
        }

        [Fact]
        public async Task GetProposalsForStaffAsync_WithValidParams_ReturnsPagedList()
        {
            // Arrange
            var staffId = "staff1";
            var queryParams = new ProposalQueryParams { PageNumber = 1, PageSize = 10 };
            var proposals = new List<Proposal> 
            { 
                new Proposal { ProposalId = "p1", Content = "Test", Status = "Pending", CreatedAt = DateTime.UtcNow, Resident = new User { Name = "Res", PasswordHash = "hash" }, ResidentId = "u1" } 
            }.AsQueryable();

            // Mock returning IQueryable backed by list
            _mockProposalRepo.Setup(r => r.GetStaffAssignedProposalsAsync(staffId))
                .Returns(new TestAsyncEnumerable<Proposal>(proposals).AsQueryable());

            // Act
            var result = await _service.GetProposalsForStaffAsync(staffId, queryParams);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetProposalsForStaffAsync_WithStatusFilter_ReturnsFilteredList()
        {
            // Arrange
            var staffId = "staff1";
            var queryParams = new ProposalQueryParams { PageNumber = 1, PageSize = 10, Status = "Pending" };
            var proposals = new List<Proposal> 
            { 
                new Proposal { ProposalId = "p1", Status = "Pending", Resident = new User { Name = "Res", PasswordHash = "hash" }, Content = "Content", ResidentId = "u1" },
                new Proposal { ProposalId = "p2", Status = "Completed", Resident = new User { Name = "Res", PasswordHash = "hash" }, Content = "Content", ResidentId = "u1" }
            }.AsQueryable();

            _mockProposalRepo.Setup(r => r.GetStaffAssignedProposalsAsync(staffId))
                .Returns(new TestAsyncEnumerable<Proposal>(proposals).AsQueryable());

            // Act
            var result = await _service.GetProposalsForStaffAsync(staffId, queryParams);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().ProposalId.Should().Be("p1");
        }

        [Fact]
        public async Task GetProposalDetailAsync_AsOwner_ReturnsDetail()
        {
            // Arrange
            var proposalId = "p1";
            var userId = "u1";
            var proposal = new Proposal { ProposalId = proposalId, ResidentId = userId, Resident = new User { Name = "Res", PasswordHash = "hash" }, Content = "Content" };
            var user = new User { UserId = userId, Role = new Role { RoleName = "Resident" }, PasswordHash = "hash", Name = "User" };

            _mockProposalRepo.Setup(r => r.GetProposalDetailsAsync(proposalId)).ReturnsAsync(proposal);
            
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetProposalDetailAsync(proposalId, userId);

            // Assert
            result.Should().NotBeNull();
            result!.ProposalId.Should().Be(proposalId);
        }

        [Fact]
        public async Task GetProposalDetailAsync_AsUnauthorizedUser_ThrowsException()
        {
            // Arrange
            var proposalId = "p1";
            var userId = "u2"; // Not owner, not assigned
            var proposal = new Proposal { ProposalId = proposalId, ResidentId = "u1", OperationStaffId = "staff1", Content = "Content" };
            var user = new User { UserId = userId, Role = new Role { RoleName = "Resident" }, PasswordHash = "hash", Name = "User" };

            _mockProposalRepo.Setup(r => r.GetProposalDetailsAsync(proposalId)).ReturnsAsync(proposal);
            
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _service.GetProposalDetailAsync(proposalId, userId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task ReplyProposalAsync_AsStaff_UpdatesProposal()
        {
            // Arrange
            var proposalId = "p1";
            var staffId = "staff1";
            var replyDto = new ProposalReplyDto { ReplyContent = "Reply" };
            var proposal = new Proposal { ProposalId = proposalId, Status = "Pending", Resident = new User { Name = "Res", PasswordHash = "hash" }, Content = "Content", ResidentId = "u1" };
            var staff = new User { UserId = staffId, Role = new Role { RoleName = "Staff" }, PasswordHash = "hash", Name = "Staff" };

            _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
            _mockProposalRepo.Setup(r => r.GetProposalDetailsAsync(proposalId)).ReturnsAsync(proposal);
            
            await _context.Users.AddAsync(staff);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ReplyProposalAsync(proposalId, staffId, replyDto);

            // Assert
            result.Should().NotBeNull();
            result!.Reply.Should().Be("Reply");
            result.Status.Should().Be("Completed");
            _mockProposalRepo.Verify(r => r.UpdateAsync(proposal), Times.Once);
        }

        [Fact]
        public async Task ReplyProposalAsync_AsNonStaff_ThrowsException()
        {
            // Arrange
            var proposalId = "p1";
            var userId = "u1";
            var replyDto = new ProposalReplyDto { ReplyContent = "Reply" };
            var proposal = new Proposal { ProposalId = proposalId, Content = "Content", ResidentId = "u1" };
            var user = new User { UserId = userId, Role = new Role { RoleName = "Resident" }, PasswordHash = "hash", Name = "User" };

            _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
            
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _service.ReplyProposalAsync(proposalId, userId, replyDto);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
    }
}

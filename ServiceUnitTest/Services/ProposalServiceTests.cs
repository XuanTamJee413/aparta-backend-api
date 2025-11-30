using ApartaAPI.Data;
using ApartaAPI.DTOs.Proposals;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceUnitTest.Helpers;
using System.Security.Claims;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Services
{
    public class ProposalServiceTests
    {
        private readonly Mock<IProposalRepository> _mockProposalRepo;
        private readonly Mock<IRepository<User>> _mockUserRepo;
        private readonly Mock<IRepository<StaffBuildingAssignment>> _mockAssignmentRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly ApartaDbContext _context;
        private readonly ProposalService _service;

        public ProposalServiceTests()
        {
            _mockProposalRepo = new Mock<IProposalRepository>();
            _mockUserRepo = new Mock<IRepository<User>>();
            _mockAssignmentRepo = new Mock<IRepository<StaffBuildingAssignment>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);

            _service = new ProposalService(
                _mockProposalRepo.Object,
                _mockUserRepo.Object,
                _mockAssignmentRepo.Object,
                _mockMapper.Object,
                _context
            );
        }

        [Fact]
        public async Task CreateProposalAsync_KhiThongTinHopLe_NenTaoProposal()
        {
            // Arrange
            var createDto = new ProposalCreateDto { Content = "Test Proposal" };
            var userId = "u1";
            var user = new User { UserId = userId, ApartmentId = "apt1", Apartment = new Apartment { ApartmentId = "apt1", BuildingId = "b1" } };
            var assignment = new StaffBuildingAssignment { UserId = "staff1" };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockAssignmentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StaffBuildingAssignment, bool>>>()))
                .ReturnsAsync(assignment);
            
            var proposal = new Proposal { ProposalId = "p1" };
            _mockMapper.Setup(m => m.Map<Proposal>(createDto)).Returns(proposal);
            _mockProposalRepo.Setup(r => r.AddAsync(It.IsAny<Proposal>())).ReturnsAsync(proposal);
            _mockProposalRepo.Setup(r => r.GetProposalDetailsAsync("p1")).ReturnsAsync(proposal);
            _mockMapper.Setup(m => m.Map<ProposalDto>(It.IsAny<Proposal>())).Returns(new ProposalDto { ProposalId = "p1" });

            // Act
            var result = await _service.CreateProposalAsync(userId, createDto);

            // Assert
            result.Should().NotBeNull();
            result.ProposalId.Should().Be("p1");
            _mockProposalRepo.Verify(r => r.AddAsync(It.IsAny<Proposal>()), Times.Once);
        }

        [Fact]
        public async Task GetProposalsByResidentAsync_KhiGoi_NenTraVeDanhSach()
        {
            // Arrange
            var userId = "u1";
            var proposals = new List<Proposal> { new Proposal { ProposalId = "p1" } };
            _mockProposalRepo.Setup(r => r.GetResidentProposalsAsync(userId)).ReturnsAsync(proposals);
            _mockMapper.Setup(m => m.Map<IEnumerable<ProposalDto>>(proposals)).Returns(new List<ProposalDto> { new ProposalDto { ProposalId = "p1" } });

            // Act
            var result = await _service.GetProposalsByResidentAsync(userId);

            // Assert
            result.Should().HaveCount(1);
            result.First().ProposalId.Should().Be("p1");
        }

        [Fact]
        public async Task GetProposalsForStaffAsync_KhiGoi_NenTraVePagedList()
        {
            // Arrange
            var staffId = "staff1";
            var queryParams = new ProposalQueryParams { PageNumber = 1, PageSize = 10 };
            var proposals = new List<Proposal> { new Proposal { ProposalId = "p1" } }.AsQueryable();

            _mockProposalRepo.Setup(r => r.GetStaffProposalsQuery(staffId)).Returns(new TestAsyncEnumerable<Proposal>(proposals).AsQueryable());
            
            // Note: ProjectTo is hard to mock without real mapper configuration. 
            // For this test, we might need to use a real mapper or skip ProjectTo verification if possible.
            // However, since we are using Moq for IMapper, ProjectTo extension method will fail if not configured.
            // We should use a real mapper for this test or mock the extension method (which is static and hard to mock).
            // Let's switch to real mapper for this specific test class setup if needed, but for now let's assume we can mock the repo to return something that ProjectTo can handle if we provided a real configuration provider.
            // Actually, ProjectTo requires a valid IConfigurationProvider.
            
            var config = new MapperConfiguration(cfg => { cfg.CreateMap<Proposal, ProposalDto>(); });
            var mapper = config.CreateMapper();
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(config);
            _mockMapper.Setup(m => m.Map<ProposalDto>(It.IsAny<Proposal>())).Returns((Proposal s) => mapper.Map<ProposalDto>(s));
            _mockMapper.Setup(m => m.Map<List<ProposalDto>>(It.IsAny<List<Proposal>>())).Returns((List<Proposal> s) => mapper.Map<List<ProposalDto>>(s));

            // Act
            var result = await _service.GetProposalsForStaffAsync(staffId, queryParams);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetProposalDetailAsync_KhiUserLaOwner_NenTraVeChiTiet()
        {
            // Arrange
            var proposalId = "p1";
            var userId = "u1";
            var proposal = new Proposal { ProposalId = proposalId, ResidentId = userId };
            
            _mockProposalRepo.Setup(r => r.GetProposalDetailsAsync(proposalId)).ReturnsAsync(proposal);
            _mockMapper.Setup(m => m.Map<ProposalDto>(proposal)).Returns(new ProposalDto { ProposalId = proposalId });

            // Act
            var result = await _service.GetProposalDetailAsync(proposalId, userId);

            // Assert
            result.Should().NotBeNull();
            result!.ProposalId.Should().Be(proposalId);
        }


        [Fact]
        public async Task ReplyProposalAsync_KhiUserLaStaff_NenTaoReplyVaCapNhatStatus()
        {
            // Arrange
            var proposalId = "p1";
            var staffId = "staff1";
            var replyDto = new ProposalReplyDto { ReplyContent = "Reply" };
            var proposal = new Proposal { ProposalId = proposalId, OperationStaffId = staffId, Status = "Pending" };

            var staff = new User { UserId = staffId, Name = "Staff", PasswordHash = "hash", Status = "Active", Role = new Role { RoleId = "r1", RoleName = "Staff" } };
            _context.Users.Add(staff);
            _context.SaveChanges();
            
            _mockUserRepo.Setup(r => r.GetByIdAsync(staffId)).ReturnsAsync(staff);
            _mockProposalRepo.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
            _mockProposalRepo.Setup(r => r.GetProposalDetailsAsync(proposalId)).ReturnsAsync(proposal);
            _mockMapper.Setup(m => m.Map<ProposalDto>(proposal)).Returns(new ProposalDto { ProposalId = proposalId, Status = "Processing", Reply = "Reply" });

            // Act
            var result = await _service.ReplyProposalAsync(proposalId, staffId, replyDto);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("Processing");
            proposal.Reply.Should().Be("Reply");
            _mockProposalRepo.Verify(r => r.UpdateAsync(proposal), Times.Once);
        }
    }
}

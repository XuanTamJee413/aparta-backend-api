using ApartaAPI.Data;
using ApartaAPI.DTOs.Chat;
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
    public class ChatServiceTests
    {
        private readonly Mock<IInteractionRepository> _mockInteractionRepo;
        private readonly Mock<IMessageRepository> _mockMessageRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ApartaDbContext _context;
        private readonly ChatService _service;

        public ChatServiceTests()
        {
            _mockInteractionRepo = new Mock<IInteractionRepository>();
            _mockMessageRepo = new Mock<IMessageRepository>();
            _mockMapper = new Mock<IMapper>();

            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);

            _service = new ChatService(
                _context,
                _mockInteractionRepo.Object,
                _mockMessageRepo.Object
            );
        }

        [Fact]
        public async Task CreateAdHocInteractionAsync_KhiInteractionTonTai_NenTraVeExisting()
        {
            // Arrange
            var residentId = "res1";
            var staffId = "staff1";
            var resident = new User { UserId = residentId, Name = "Resident", Role = new Role { RoleName = "Resident" } };
            var staff = new User { UserId = staffId, Name = "Staff", Role = new Role { RoleName = "Staff" } };
            var existingInteraction = new Interaction { InteractionId = "int1", ResidentId = residentId, StaffId = staffId, Resident = resident, Staff = staff };
            
            _mockInteractionRepo.Setup(r => r.GetInteractionByParticipantsAsync(residentId, staffId)).ReturnsAsync(existingInteraction);
            _mockMapper.Setup(m => m.Map<InitiateInteractionDto>(It.IsAny<Interaction>())).Returns(new InitiateInteractionDto { InteractionId = "int1" });

            // Act
            var result = await _service.CreateAdHocInteractionAsync(residentId, staffId);

            // Assert
            result.Should().NotBeNull();
            result.InteractionId.Should().Be("int1");
            _mockInteractionRepo.Verify(r => r.AddAsync(It.IsAny<Interaction>()), Times.Never);
        }

        [Fact]
        public async Task CreateAdHocInteractionAsync_KhiChuaTonTai_NenTaoMoi()
        {
            // Arrange
            var residentId = "res1";
            var staffId = "staff1";
            
            var role1 = new Role { RoleId = "r1", RoleName = "Resident" };
            var role2 = new Role { RoleId = "r2", RoleName = "Staff" };
            var resident = new User { UserId = residentId, Name = "Resident", PasswordHash = "hash", Status = "Active", Role = role1 };
            var staff = new User { UserId = staffId, Name = "Staff", PasswordHash = "hash", Status = "Active", Role = role2 };
            
            await _context.Roles.AddRangeAsync(role1, role2);
            await _context.Users.AddRangeAsync(resident, staff);
            await _context.SaveChangesAsync();

            _mockInteractionRepo.Setup(r => r.GetInteractionByParticipantsAsync(residentId, staffId)).ReturnsAsync((Interaction)null);
            _mockMapper.Setup(m => m.Map<InitiateInteractionDto>(It.IsAny<Interaction>())).Returns(new InitiateInteractionDto { InteractionId = "new" });

            // Act
            var result = await _service.CreateAdHocInteractionAsync(residentId, staffId);

            // Assert
            result.Should().NotBeNull();
            result.InteractionId.Should().Be("new");
            _mockInteractionRepo.Verify(r => r.AddAsync(It.IsAny<Interaction>()), Times.Once);
        }

        [Fact]
        public async Task GetInteractionListAsync_KhiGoi_NenTraVeDanhSach()
        {
            // Arrange
            var userId = "u1";
            var partnerId = "staff1";
            var resident = new User { UserId = userId, Name = "Resident" };
            var staff = new User { UserId = partnerId, Name = "Staff" };
            var interactions = new List<Interaction> { new Interaction { InteractionId = "int1", ResidentId = userId, StaffId = partnerId, Resident = resident, Staff = staff, Messages = new List<Message>() } };
            
            _mockInteractionRepo.Setup(r => r.GetUserInteractionsWithMessagesAsync(userId)).ReturnsAsync(interactions);
            _mockMessageRepo.Setup(r => r.GetUnreadCountForInteractionAsync("int1", userId)).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<InteractionListDto>(It.IsAny<Interaction>())).Returns(new InteractionListDto { InteractionId = "int1" });

            // Act
            var result = await _service.GetInteractionListAsync(userId);

            // Assert
            result.Should().HaveCount(1);
            result.First().UnreadCount.Should().Be(1);
        }

        [Fact]
        public async Task GetMessageHistoryAsync_KhiGoi_NenTraVeTinNhanVaMarkRead()
        {
            // Arrange
            var interactionId = "int1";
            var userId = "u1";
            var messages = new List<Message> { new Message { MessageId = "m1" } };
            var resident = new User { UserId = userId, Name = "Resident", PasswordHash = "hash", Status = "Active", Role = new Role { RoleId = "r1", RoleName = "Resident" } };
            var staff = new User { UserId = "staff1", Name = "Staff", PasswordHash = "hash", Status = "Active", Role = new Role { RoleId = "r2", RoleName = "Staff" } };
            var interaction = new Interaction { InteractionId = interactionId, ResidentId = userId, StaffId = "staff1", Resident = resident, Staff = staff };
            
            await _context.Users.AddRangeAsync(resident, staff);
            await _context.Interactions.AddAsync(interaction);
            await _context.SaveChangesAsync();

            _mockInteractionRepo.Setup(r => r.GetByIdAsync(interactionId)).ReturnsAsync(interaction);
            _mockMessageRepo.Setup(r => r.GetMessagesAsync(interactionId, 0, 20)).ReturnsAsync(messages);
            _mockMapper.Setup(m => m.Map<IEnumerable<MessageDetailDto>>(messages)).Returns(new List<MessageDetailDto> { new MessageDetailDto { MessageId = "m1" } });

            // Act
            var result = await _service.GetMessageHistoryAsync(interactionId, userId, 1, 20);

            // Assert
            result.Should().HaveCount(1);
            _mockMessageRepo.Verify(r => r.MarkAsReadAsync(interactionId, userId), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_KhiThongTinHopLe_NenGuiTinNhan()
        {
            // Arrange
            var sendDto = new SendMessageDto { InteractionId = "int1", Content = "Hello" };
            var userId = "u1";
            var interaction = new Interaction { InteractionId = "int1" };
            
            _mockInteractionRepo.Setup(r => r.GetByIdAsync("int1")).ReturnsAsync(interaction);
            _mockMessageRepo.Setup(r => r.AddMessageAsync(It.IsAny<Message>())).ReturnsAsync(new Message { MessageId = "m1" });

            // Act
            var result = await _service.SendMessageAsync(userId, sendDto);

            // Assert
            result.Message.Should().NotBeNull();
            result.Message!.MessageId.Should().Be("m1");
        }

        [Fact]
        public async Task SearchPotentialPartnersAsync_KhiLaResident_NenTraVeStaff()
        {
            // Arrange
            var userId = "res1";
            var buildingId = "b1";
            var building = new Building { BuildingId = buildingId, BuildingCode = "B1", Name = "Building 1", ProjectId = "proj1" };
            var resident = new User { UserId = userId, Name = "Res 1", PasswordHash = "hash", Status = "Active", Role = new Role { RoleId = "r1", RoleName = "Resident" }, Apartment = new Apartment { ApartmentId = "apt1", BuildingId = buildingId, Code = "A101", Building = building, Status = "Occupied" } };
            var staff = new User { UserId = "staff1", Name = "Staff 1", PasswordHash = "hash", Status = "Active", Role = new Role { RoleId = "r2", RoleName = "Staff" } };
            var assignment = new StaffBuildingAssignment { AssignmentId = "a1", UserId = "staff1", BuildingId = buildingId, IsActive = true, AssignmentStartDate = DateOnly.FromDateTime(DateTime.Now), AssignmentEndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(1)), Building = building, User = staff };

            await _context.Buildings.AddAsync(building);
            await _context.Users.AddRangeAsync(resident, staff);
            await _context.StaffBuildingAssignments.AddAsync(assignment);
            await _context.SaveChangesAsync();

            _mockMapper.Setup(m => m.Map<PartnerDto>(It.IsAny<User>())).Returns(new PartnerDto { UserId = "staff1" });

            // Act
            var result = await _service.SearchPotentialPartnersAsync(userId);

            // Assert
            result.Should().HaveCount(1);
            result.First().UserId.Should().Be("staff1");
        }
    }
}

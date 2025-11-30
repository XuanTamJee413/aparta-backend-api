using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Repositories
{
    public class MessageRepositoryTests
    {
        private readonly ApartaDbContext _context;
        private readonly MessageRepository _repository;

        public MessageRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);
            _repository = new MessageRepository(_context);
        }

        [Fact]
        public async Task GetMessagesAsync_KhiGoi_NenTraVeTinNhanPhanTrang()
        {
            // Arrange
            var interactionId = "int1";
            var messages = new List<Message>
            {
                new Message { MessageId = "m1", InteractionId = interactionId, SentAt = DateTime.Now.AddMinutes(-10), Content = "Msg 1", SenderId = "u1", IsRead = false },
                new Message { MessageId = "m2", InteractionId = interactionId, SentAt = DateTime.Now.AddMinutes(-5), Content = "Msg 2", SenderId = "u2", IsRead = false },
                new Message { MessageId = "m3", InteractionId = interactionId, SentAt = DateTime.Now, Content = "Msg 3", SenderId = "u1", IsRead = false }
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetMessagesAsync(interactionId, 1, 2);

            // Assert
            result.Should().HaveCount(2);
            result.First().MessageId.Should().Be("m1");
            result.Last().MessageId.Should().Be("m2");
        }

        [Fact]
        public async Task AddMessageAsync_KhiMessageHopLe_NenThemVaCapNhatInteraction()
        {
            // Arrange
            var interactionId = "int1";
            var interaction = new Interaction { InteractionId = interactionId, ResidentId = "res1", StaffId = "staff1", CreatedAt = DateTime.Now.AddDays(-1), UpdatedAt = DateTime.Now.AddDays(-1) };
            await _context.Interactions.AddAsync(interaction);
            await _context.SaveChangesAsync();

            var message = new Message { InteractionId = interactionId, SenderId = "res1", Content = "New Msg", IsRead = false };

            // Act
            var result = await _repository.AddMessageAsync(message);

            // Assert
            result.MessageId.Should().NotBeNull();
            result.SentAt.Should().NotBe(default);
            
            var updatedInteraction = await _context.Interactions.FindAsync(interactionId);
            updatedInteraction.Should().NotBeNull();
            updatedInteraction!.UpdatedAt.Should().HaveValue();
            updatedInteraction.UpdatedAt!.Value.Should().BeAfter(DateTime.Now.AddMinutes(-1));
        }

        [Fact]
        public async Task MarkAsReadAsync_KhiCoTinNhanChuaDoc_NenCapNhatIsRead()
        {
            // Arrange
            var interactionId = "int1";
            var readerId = "u1";
            var senderId = "u2";
            var messages = new List<Message>
            {
                new Message { MessageId = "m1", InteractionId = interactionId, SenderId = senderId, IsRead = false, Content = "Msg 1", SentAt = DateTime.Now },
                new Message { MessageId = "m2", InteractionId = interactionId, SenderId = senderId, IsRead = true, Content = "Msg 2", SentAt = DateTime.Now }, // Already read
                new Message { MessageId = "m3", InteractionId = interactionId, SenderId = readerId, IsRead = false, Content = "Msg 3", SentAt = DateTime.Now } // Sent by reader
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            var count = await _repository.MarkAsReadAsync(interactionId, readerId);

            // Assert
            count.Should().Be(1);
            var m1 = await _context.Messages.FindAsync("m1");
            m1!.IsRead.Should().BeTrue();
        }

        [Fact]
        public async Task GetUnreadCountForInteractionAsync_KhiGoi_NenTraVeSoLuongChuaDoc()
        {
            // Arrange
            var interactionId = "int1";
            var readerId = "u1";
            var senderId = "u2";
            var messages = new List<Message>
            {
                new Message { MessageId = "m1", InteractionId = interactionId, SenderId = senderId, IsRead = false, Content = "Msg 1", SentAt = DateTime.Now },
                new Message { MessageId = "m2", InteractionId = interactionId, SenderId = senderId, IsRead = true, Content = "Msg 2", SentAt = DateTime.Now },
                new Message { MessageId = "m3", InteractionId = interactionId, SenderId = readerId, IsRead = false, Content = "Msg 3", SentAt = DateTime.Now }
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            var count = await _repository.GetUnreadCountForInteractionAsync(interactionId, readerId);

            // Assert
            count.Should().Be(1);
        }

        [Fact]
        public async Task GetTotalUnreadCountAsync_KhiGoi_NenTraVeTongSoLuongChuaDoc()
        {
            // Arrange
            var readerId = "u1";
            var senderId = "u2";
            
            var int1 = new Interaction { InteractionId = "int1", ResidentId = readerId, StaffId = senderId };
            var int2 = new Interaction { InteractionId = "int2", ResidentId = readerId, StaffId = senderId };
            
            var messages = new List<Message>
            {
                new Message { MessageId = "m1", InteractionId = "int1", SenderId = senderId, IsRead = false, Content = "Msg 1", SentAt = DateTime.Now, Interaction = int1 },
                new Message { MessageId = "m2", InteractionId = "int2", SenderId = senderId, IsRead = false, Content = "Msg 2", SentAt = DateTime.Now, Interaction = int2 },
                new Message { MessageId = "m3", InteractionId = "int1", SenderId = readerId, IsRead = false, Content = "Msg 3", SentAt = DateTime.Now, Interaction = int1 }
            };
            
            await _context.Interactions.AddRangeAsync(int1, int2);
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            var count = await _repository.GetTotalUnreadCountAsync(readerId);

            // Assert
            count.Should().Be(2);
        }
    }
}

using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Repositories
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        public MessageRepository(ApartaDbContext context) : base(context)
        {
        }

        // Triển khai AddMessageCustomAsync (vì logic thêm message cần set ID và SentAt)
        public async Task<IEnumerable<Message>> GetMessagesAsync(string interactionId, int skip, int take)
        {
            // Load messages theo cơ chế phân trang (tải tin nhắn cũ)
            return await _context.Messages
                .Where(m => m.InteractionId == interactionId)
                .OrderByDescending(m => m.SentAt) // Bắt đầu từ tin nhắn mới nhất
                .Skip(skip)
                .Take(take)
                .OrderBy(m => m.SentAt) // Sắp xếp lại từ cũ -> mới cho Frontend
                .ToListAsync();
        }
        public async Task<Message> AddMessageAsync(Message message)
        {
            message.MessageId = Guid.NewGuid().ToString();
            message.SentAt = DateTime.Now;

            await _dbSet.AddAsync(message);

            // Cập nhật thời gian cuối cùng của Interaction (cần truy cập _context)
            var interaction = await _context.Interactions.FindAsync(message.InteractionId);
            if (interaction != null)
            {
                interaction.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<int> MarkAsReadAsync(string interactionId, string readerId)
        {
            // Đánh dấu tất cả tin nhắn chưa đọc, mà không phải do người dùng hiện tại gửi.
            var messages = await _context.Messages
                .Where(m => m.InteractionId == interactionId &&
                            m.SenderId != readerId &&
                            m.IsRead == false)
                .ToListAsync();

            if (messages.Any())
            {
                messages.ForEach(m => m.IsRead = true);
                return await _context.SaveChangesAsync();
            }
            return 0;
        }

        public async Task<int> GetUnreadCountForInteractionAsync(string interactionId, string readerId)
        {
            return await _context.Messages
                .CountAsync(m => m.InteractionId == interactionId &&
                                 m.SenderId != readerId &&
                                 m.IsRead == false);
        }

        public async Task<int> GetTotalUnreadCountAsync(string readerId)
        {
            return await _context.Messages
                .CountAsync(m => (m.Interaction.ResidentId == readerId || m.Interaction.StaffId == readerId) &&
                                 m.SenderId != readerId &&
                                 m.IsRead == false);
        }
    }
}

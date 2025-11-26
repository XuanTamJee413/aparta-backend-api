using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<IEnumerable<Message>> GetMessagesAsync(string interactionId, int skip, int take);
        // AddMessageAsync được thay bằng AddAsync, nhưng cần logic cập nhật SentAt
        Task<Message> AddMessageAsync(Message message);
        Task<int> MarkAsReadAsync(string interactionId, string readerId);
        Task<int> GetUnreadCountForInteractionAsync(string interactionId, string readerId);
        Task<int> GetTotalUnreadCountAsync(string readerId);
    }
}

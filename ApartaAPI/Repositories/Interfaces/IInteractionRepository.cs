using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IInteractionRepository : IRepository<Interaction>
    {
        // Giữ lại các phương thức CHUYÊN BIỆT cho Chat:
        Task<Interaction?> GetInteractionByParticipantsAsync(string residentId, string staffId);

        // Cần phương thức phức tạp để lấy danh sách chat (JOIN Messages & Users)
        Task<IEnumerable<Interaction>> GetUserInteractionsWithMessagesAsync(string userId);
    }
}

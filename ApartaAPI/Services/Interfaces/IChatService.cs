using ApartaAPI.DTOs.Chat;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IChatService
    {
        // Khởi tạo/Tìm Chat
        Task<InitiateInteractionDto> InitiateOrGetInteractionAsync(string currentUserId);

        // Danh sách Chat
        Task<IEnumerable<InteractionListDto>> GetInteractionListAsync(string currentUserId);

        // Lấy tin nhắn
        Task<IEnumerable<MessageDetailDto>> GetMessageHistoryAsync(string interactionId, string currentUserId, int pageNumber, int pageSize);

        // Gửi tin nhắn
        Task<(Message? Message, string? ReceiverId)> SendMessageAsync(string currentUserId, SendMessageDto messageDto);
    }
}

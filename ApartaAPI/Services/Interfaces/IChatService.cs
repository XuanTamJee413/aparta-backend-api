using ApartaAPI.DTOs.Chat;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IChatService
    {
        // Khởi tạo/Tìm Chat
        Task<InitiateInteractionDto> CreateAdHocInteractionAsync(string currentUserId, string partnerId);
        // Danh sách Chat
        Task<IEnumerable<InteractionListDto>> GetInteractionListAsync(string currentUserId);

        // Lấy tin nhắn
        Task<PagedList<MessageDetailDto>> GetMessageHistoryAsync(string interactionId, string currentUserId, ChatQueryParameters queryParams);
        // Gửi tin nhắn
        Task<(Message? Message, string? ReceiverId)> SendMessageAsync(string currentUserId, SendMessageDto messageDto);

        // lấy danh sách partner đổ vô combobozx
        Task<IEnumerable<PartnerDto>> SearchPotentialPartnersAsync(string currentUserId);
    }
}

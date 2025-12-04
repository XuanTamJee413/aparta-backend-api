using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Chat
{
    public class PartnerDto
    {
        public string UserId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = null!;

        public string? ApartmentCode { get; set; }
    }
    public class InteractionListDto
    {
        public string InteractionId { get; set; } = null!;
        public string PartnerId { get; set; } = null!;
        public string PartnerName { get; set; } = null!;
        public string? PartnerAvatarUrl { get; set; }
        public string? LastMessageContent { get; set; }
        public DateTime? LastMessageSentAt { get; set; }
        public int UnreadCount { get; set; }
    }
    public class MessageDetailDto
    {
        public string MessageId { get; set; } = null!;
        public string SenderId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
    public class SendMessageDto
    {
        [Required]
        public string InteractionId { get; set; } = null!;

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = null!;
    }
    public class InitiateInteractionDto
    {
        public string InteractionId { get; set; } = null!;
        public string PartnerId { get; set; } = null!;
        public string PartnerName { get; set; } = null!;
    }
    public class CreateAdHocInteractionDto
    {
        public string PartnerId { get; set; } = null!;
    }
}

using ApartaAPI.DTOs.Chat;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        // Khai báo SignalR Hub Context (Cần thiết cho Real-time)
        // private readonly IHubContext<ChatHub> _hubContext; 

        public ChatController(IChatService chatService /*, IHubContext<ChatHub> hubContext */)
        {
            _chatService = chatService;
            // _hubContext = hubContext;
        }

        // ===================================================================
        // 1. KHỞI TẠO HOẶC TÌM CUỘC HỘI THOẠI (Resident)
        // POST: api/Chat/initiate-interaction
        // ===================================================================
        [HttpPost("initiate-interaction")]
        public async Task<ActionResult<InitiateInteractionDto>> InitiateInteraction()
        {
            try
            {
                // Lấy UserID từ Claims/Token
                var currentUserId = User.FindFirst("id")?.Value;
                if (currentUserId == null) return Unauthorized();

                var result = await _chatService.InitiateOrGetInteractionAsync(currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi: Không tìm thấy Staff, không gán căn hộ, vv.
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ===================================================================
        // 2. LẤY DANH SÁCH CUỘC HỘI THOẠI (Cột Trái)
        // GET: api/Chat/interactions
        // ===================================================================
        [HttpGet("interactions")]
        public async Task<ActionResult<IEnumerable<InteractionListDto>>> GetInteractionsList()
        {
            var currentUserId = User.FindFirst("id")?.Value;
            if (currentUserId == null) return Unauthorized();

            var interactions = await _chatService.GetInteractionListAsync(currentUserId);
            return Ok(interactions);
        }

        // ===================================================================
        // 3. LẤY LỊCH SỬ TIN NHẮN (Phân trang - Load More)
        // GET: api/Chat/interactions/{interactionId}/messages?pageNumber=1&pageSize=10
        // ===================================================================
        [HttpGet("interactions/{interactionId}/messages")]
        public async Task<ActionResult<IEnumerable<MessageDetailDto>>> GetMessages(
            string interactionId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10) // Mặc định lấy 10 tin nhắn mới nhất
        {
            try
            {
                var currentUserId = User.FindFirst("id")?.Value;
                if (currentUserId == null) return Unauthorized();

                var messages = await _chatService.GetMessageHistoryAsync(interactionId, currentUserId, pageNumber, pageSize);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception)
            {
                return NotFound("Cuộc hội thoại không tồn tại.");
            }
        }

        // ===================================================================
        // 4. GỬI TIN NHẮN MỚI
        // POST: api/Chat/messages
        // ===================================================================
        [HttpPost("messages")]
        public async Task<ActionResult<MessageDetailDto>> SendMessage([FromBody] SendMessageDto messageDto)
        {
            var currentUserId = User.FindFirst("id")?.Value;
            if (currentUserId == null) return Unauthorized();

            var (message, receiverId) = await _chatService.SendMessageAsync(currentUserId, messageDto);

            if (message == null || receiverId == null)
            {
                return BadRequest("Không thể gửi tin nhắn. Interaction không tồn tại hoặc không hợp lệ.");
            }

            var responseDto = new MessageDetailDto
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                Content = message.Content,
                SentAt = message.SentAt,
                IsRead = message.IsRead
            };

            // **SIGNALR:** Gửi thông báo ngay lập tức đến người nhận
            // Cần thêm logic SignalR để gửi message và cập nhật UnreadCount cho người nhận.

            // Ví dụ: await _hubContext.Clients.User(receiverId).SendAsync("ReceiveMessage", responseDto);
            // và: await _hubContext.Clients.User(receiverId).SendAsync("UpdateUnreadCount");

            return CreatedAtAction(nameof(GetMessages),
                new { interactionId = messageDto.InteractionId },
                responseDto);
        }
    }
}
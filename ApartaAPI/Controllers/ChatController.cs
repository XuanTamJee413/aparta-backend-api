using ApartaAPI.DTOs.Chat;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Hubs;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
         private readonly IHubContext<ChatHub> _hubContext; 

        public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

        // ===================================================================
        // 1. TẠO HOẶC TÌM CUỘC HỘI THOẠI AD-HOC (SỬ DỤNG CHO COMBOBOX)
        // POST: api/Chat/create-interaction
        // ===================================================================
        [HttpPost("create-interaction")]
        public async Task<ActionResult<InitiateInteractionDto>> CreateAdHocInteraction([FromBody] CreateAdHocInteractionDto dto)
        {
            try
            {
                var currentUserId = User.FindFirst("id")?.Value;
                if (currentUserId == null) return Unauthorized();

                // Gọi Service mới (đã được sửa trong ChatService.cs)
                var result = await _chatService.CreateAdHocInteractionAsync(currentUserId, dto.PartnerId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi server: " + ex.Message });
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
        public async Task<ActionResult<ApiResponse<PagedList<MessageDetailDto>>>> GetMessages(
            string interactionId,
            [FromQuery] ChatQueryParameters queryParams) // [CHUẨN HÓA] Dùng Object param
        {
            try
            {
                var currentUserId = User.FindFirst("id")?.Value;
                if (currentUserId == null) return Unauthorized(ApiResponse.Fail("User ID not found."));

                // Gọi Service đã refactor
                var pagedMessages = await _chatService.GetMessageHistoryAsync(interactionId, currentUserId, queryParams);

                // [CHUẨN HÓA] Trả về ApiResponse
                if (pagedMessages.TotalCount == 0)
                {
                    return Ok(ApiResponse<PagedList<MessageDetailDto>>.Success(pagedMessages, ApiResponse.SM01_NO_RESULTS));
                }

                return Ok(ApiResponse<PagedList<MessageDetailDto>>.Success(pagedMessages));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail(ApiResponse.SM40_SYSTEM_ERROR, null, ex.Message));
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

            // 1. Gửi tin nhắn mới đến người nhận
            await _hubContext.Clients.User(receiverId).SendAsync("ReceiveMessage", responseDto);

            // 2. Gửi thông báo cập nhật danh sách chat (UnreadCount) cho cả hai bên
            await _hubContext.Clients.User(currentUserId).SendAsync("UpdateChatList"); // Cập nhật cho người gửi (để thấy tin nhắn mới nhất)
            await _hubContext.Clients.User(receiverId).SendAsync("UpdateChatList"); // Cập nhật cho người nhận (để thấy UnreadCount)

            return CreatedAtAction(nameof(GetMessages),
                new { interactionId = messageDto.InteractionId },
                responseDto);
        }

        [HttpGet("search-partners")]
        [ProducesResponseType(typeof(IEnumerable<PartnerDto>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<ActionResult<IEnumerable<PartnerDto>>> SearchPartners()
        {
            var currentUserId = User.FindFirst("id")?.Value;
            if (currentUserId == null) return Unauthorized();

            // Gọi Service để tìm kiếm đối tác dựa trên Role và Building ID của người dùng hiện tại
            var partners = await _chatService.SearchPotentialPartnersAsync(currentUserId);

            return Ok(partners);
        }
    }
}
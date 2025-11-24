using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ApartaAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // Hub không cần logic phức tạp ở đây, chỉ cần cho phép kết nối.
        // Logic gửi/nhận tin nhắn được xử lý trong ChatController.

        // Ví dụ theo dõi kết nối (tùy chọn)
        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("id")?.Value;
            Console.WriteLine($"[SignalR] User {userId} connected.");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("id")?.Value;
            Console.WriteLine($"[SignalR] User {userId} disconnected.");
            return base.OnDisconnectedAsync(exception);
        }
        public class CustomUserIdProvider : IUserIdProvider
        {
            public string? GetUserId(HubConnectionContext connection)
            {
                // Lấy User ID từ Claim "id" trong JWT Payload
                return connection.User?.FindFirst("id")?.Value;
            }
        }
    }
}

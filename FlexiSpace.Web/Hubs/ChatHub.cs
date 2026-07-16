using FlexiSpace.Application.IServices;
using Microsoft.AspNetCore.SignalR;

namespace FlexiSpace.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        public ChatHub(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessageToGroup(string conversationId, string message)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId))
            {
                throw new UnauthorizedAccessException("Không thể xác thực người dùng.");
            }

            // Lưu xuống DB. Lưu ý: Nên điều chỉnh SaveMessageAsync để trả về MessageResponse
            var savedMessage = await _messageService.SaveMessageAsync(conversationId, senderId, message);

            // Gửi cho TOÀN BỘ những ai đang kết nối vào phòng chat này
            // Đảm bảo event name "ReceiveNewMessage" khớp với Controller chia sẻ hợp đồng
            await Clients.Group(conversationId).SendAsync("ReceiveNewMessage", savedMessage.Data);
        }
        public async Task MarkConversationAsRead(string conversationId)
        {
            var currentUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(currentUserId)) return;

            bool isUpdated = await _messageService.UpdateMessagesToReadAsync(conversationId, currentUserId);

            // 2. Chỉ bắn SignalR nếu thực sự có tin nhắn vừa được đổi trạng thái
            if (isUpdated)
            {
                // Thông báo cho mọi người trong phòng biết là "Tài khoản currentUserId đã xem tin nhắn"
                await Clients.Group(conversationId).SendAsync("ReceiveReadReceipt", new
                {
                    ConversationId = conversationId,
                    ReaderId = currentUserId,
                    ReadAt = DateTime.UtcNow
                });
            }
        }
    }
}

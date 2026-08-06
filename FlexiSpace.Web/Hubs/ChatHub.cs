using FlexiSpace.Application;
using FlexiSpace.Application.Events.Notification;
using FlexiSpace.Application.IServices;
using FlexiSpace.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using MimeKit;
using Org.BouncyCastle.Tls;

namespace FlexiSpace.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IMediator _mediator;
        public ChatHub(IMessageService messageService, IMediator mediator)
        {
            _messageService = messageService;
            _mediator = mediator;
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
            var savedMessage = await _messageService.SaveMessageAsync(conversationId, senderId, message);

            await Clients.Group(conversationId).SendAsync("ReceiveNewMessage", savedMessage.Data);
            await _mediator.Publish(new ChatMessageReceivedEvent
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = message,

            });
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

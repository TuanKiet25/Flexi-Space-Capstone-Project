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
        public async Task SendMessageToUser(string receiverId, string conversationId, string message)
        {
            var senderId = Context.UserIdentifier;
            if(senderId == null)
            {
                throw new UnauthorizedAccessException();
            }
            await _messageService.SaveMessageAsync(conversationId, senderId, message);
            await Clients.User(receiverId).SendAsync("ReceiveMessage", new
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = message,
                SentAt = DateTime.UtcNow
            });
        }
    }
}

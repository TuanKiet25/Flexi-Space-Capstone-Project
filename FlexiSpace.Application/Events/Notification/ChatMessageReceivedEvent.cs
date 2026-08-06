using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Events.Notification
{
    public class ChatMessageReceivedEvent : INotification
    {
        public string? ConversationId { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public string? Content { get; set; }
        public string? SenderName { get; set; }
    }
}

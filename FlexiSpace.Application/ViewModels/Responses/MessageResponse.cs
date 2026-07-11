using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class MessageResponse
    {
        public string? Id { get; set; }
        public string? ConversationId { get; set; }
        public string? SenderId { get; set; }

        public string? Content { get; set; }

        public DateTime CreateAt { get; set; }
        public bool IsRead { get; set; }

        public MessageTypeEnum MessageType { get; set; }
    }
}

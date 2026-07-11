using FlexiSpace.Domain.Enum;

namespace FlexiSpace.Domain.Entities
{
    public class Message
    {
        public string Id { get; set; } = Ulid.NewUlid().ToString();
        public string ConversationId { get; set; }
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public MessageTypeEnum MessageType { get; set; }    
        public virtual Conversation Conversation { get; set; }
        public virtual User Sender { get; set; }
    }
}
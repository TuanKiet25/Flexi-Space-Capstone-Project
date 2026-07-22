using FlexiSpace.Domain.Enum;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class NotificationResponse
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public NotificationTypeEnum Type { get; set; }
        public string? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

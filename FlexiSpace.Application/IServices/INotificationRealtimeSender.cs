using FlexiSpace.Application.ViewModels.Responses;

namespace FlexiSpace.Application.IServices
{
    public interface INotificationRealtimeSender
    {
        Task SendToUserAsync(string userId, NotificationResponse notification);
    }
}

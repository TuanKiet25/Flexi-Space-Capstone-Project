using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;
using Microsoft.AspNetCore.SignalR;

namespace FlexiSpace.Web.Hubs
{
    public class NotificationRealtimeSender : INotificationRealtimeSender
    {
        public const string ReceiveNotificationEvent = "ReceiveNotification";
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationRealtimeSender(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task SendToUserAsync(string userId, NotificationResponse notification)
        {
            return _hubContext.Clients.User(userId)
                .SendAsync(ReceiveNotificationEvent, notification);
        }
    }
}

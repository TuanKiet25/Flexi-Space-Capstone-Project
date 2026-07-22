using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlexiSpace.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}

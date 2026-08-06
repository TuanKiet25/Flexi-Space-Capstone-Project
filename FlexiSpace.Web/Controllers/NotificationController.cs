using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : MyBaseController
    {
        private readonly INotificationExpoService _notificationExpoService;
        public NotificationController(INotificationExpoService notificationExpoService)
        {
            _notificationExpoService = notificationExpoService;
        }
        [HttpPost("save-token")]
        public async Task<IActionResult> SaveToken([FromBody] SaveTokenRequest saveTokenRequest)
        {
            var result = await _notificationExpoService.SaveToken(saveTokenRequest);
            return HandleResult(result);
        }
    }
}

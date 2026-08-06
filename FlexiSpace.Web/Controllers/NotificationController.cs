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
        private readonly INotificationService _notificationService;

        public NotificationController(
            INotificationExpoService notificationExpoService,
            INotificationService notificationService)
        {
            _notificationExpoService = notificationExpoService;
            _notificationService = notificationService;
        }

        [HttpPost("save-token")]
        public async Task<IActionResult> SaveToken([FromBody] SaveTokenRequest saveTokenRequest)
        {
            var result = await _notificationExpoService.SaveToken(saveTokenRequest);
            return HandleResult(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var result = await _notificationService.GetHistoryByCurrentUserAsync();
            return HandleResult(result);
        }
    }
}

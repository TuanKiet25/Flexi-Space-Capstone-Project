using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : MyBaseController
    {
        private readonly IMessageService _messageService;
        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }
        [HttpGet("GetMessageHistory")]
        public async Task<IActionResult> GetMessageHistory(string conversationId, DateTime? timeBefore, int limit)
        {
            var result = await _messageService.GetMessagesAsync(conversationId, timeBefore, limit);
            return HandleResult(result);
        }

    }
}

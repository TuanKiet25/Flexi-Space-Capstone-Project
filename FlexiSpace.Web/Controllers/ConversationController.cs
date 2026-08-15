using FlexiSpace.Application.IServices;
using FlexiSpace.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationController : MyBaseController
    {
        private readonly IConversationService _conversationService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ConversationController(IConversationService conversationService, IHubContext<ChatHub> hubContext)
        {
            _conversationService = conversationService;
            _hubContext = hubContext;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateConversation(string lessorId, string lesseeId)
        {
            var result = await _conversationService.GetOrCreateConversationAsync(lessorId, lesseeId);
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data))
            {
                await _hubContext.Clients.Users(new[] { lessorId, lesseeId }).SendAsync("ReceiveNewConversation", new
                {
                    Id = result.Data,
                    LessorId = lessorId,
                    LesseeId = lesseeId
                });
            }

            return HandleResult(result);
        }

        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetConversationsByUserId(string userId)
        {
            var result = await _conversationService.GetConversationsByUserIdAsync(userId);
            return HandleResult(result);
        }

        [HttpGet("ByParticipants")]
        public async Task<IActionResult> GetConversationByParticipants([FromQuery] string lessorId, [FromQuery] string lesseeId)
        {
            var result = await _conversationService.GetConversationByParticipantsAsync(lessorId, lesseeId);
            return HandleResult(result);
        }
    }
}

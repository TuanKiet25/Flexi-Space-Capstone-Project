using FlexiSpace.Application.IServices;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationController : MyBaseController 
    {
        private readonly IConversationService _conversationService;
        public ConversationController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }
        [HttpPost("Create")]
        public async Task<IActionResult> CreateConversation(string lessorId, string lesseeId)
        {
            var result = await _conversationService.GetOrCreateConversationAsync(lessorId,lesseeId);
            return HandleResult(result);
        }
        
    }
}

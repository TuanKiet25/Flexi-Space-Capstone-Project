using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalContractController : MyBaseController
    {
        private readonly IExternalContractService _externalContractService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ExternalContractController(IExternalContractService externalContractService, IHubContext<ChatHub> hubContext)
        {
            _externalContractService = externalContractService;
            _hubContext = hubContext;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateExternalContractRequest request)
        {
            var result = await _externalContractService.CreateAndShareAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Message);
            }

            var messageData = result.Data;
            await _hubContext.Clients.Group(messageData!.ConversationId!)
                .SendAsync("ReceiveNewMessage", messageData);

            return Ok(result);
        }

        [HttpPost("{contractId:long}/Confirm")]
        public async Task<IActionResult> Confirm(long contractId)
        {
            var result = await _externalContractService.ConfirmAsync(contractId);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Message);
            }

            var messageData = result.Data;
            if (messageData?.ConversationId != null)
            {
                await _hubContext.Clients.Group(messageData.ConversationId)
                    .SendAsync("ReceiveNewMessage", messageData);
            }

            return Ok(result);
        }
    }
}

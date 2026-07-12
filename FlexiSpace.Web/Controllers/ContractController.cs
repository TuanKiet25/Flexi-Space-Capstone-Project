using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Requests.Contract;
using FlexiSpace.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : MyBaseController
    {
        private readonly IContractService _contractService;
        private readonly IHubContext<ChatHub> _hubContext;
        public ContractController(IContractService contractService, IHubContext<ChatHub> hubContext)
        {
            _contractService = contractService;
            _hubContext = hubContext;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateContract([FromBody] ContractRequest request)
        {
            var result = await _contractService.CreateContractAsync(request);
            return HandleResult(result);
        }
        [HttpPost("{id}/share")]
        public async Task<IActionResult> ShareContract(long id)
        {
            var result = await _contractService.ShareContractAsync(id);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Message);
            }

            var messageData = result.Data;

            await _hubContext.Clients.Group(messageData!.ConversationId!)
                 .SendAsync("ReceiveNewMessage", messageData);

            return Ok(result);
        }
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllContracts([FromQuery] FilterGetAllContract filter)
        {
            var result = await _contractService.GetAllContractsAsync(filter);
            return HandleResult(result);
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetContractById(long id)
        {
            var result = await _contractService.GetContractByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateContract(long id, [FromBody] ContractRequest request)
        {
            var result = await _contractService.UpdateContractAsync(id, request);
            return HandleResult(result);
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteContract(long id)
        {
            var result = await _contractService.DeleteContractAsync(id);
            return HandleResult(result);
        }

        [HttpPost("{contractId}/send-otp")]
        public async Task<IActionResult> SendContractOtp(long contractId)
        {
            var result = await _contractService.SendContractOtpAsync(contractId);
            return HandleResult(result);
        }

        [HttpPost("{contractId}/validate-otp")]
        public async Task<IActionResult> ValidateContractOtp(long id, string inputOtp)
        {
            var result = await _contractService.ContractValidateOtpAsync(id, inputOtp);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Message);
            }

            if (result.Data != null)
            {
                var messageData = result.Data;
                await _hubContext.Clients.Group(messageData!.ConversationId!.ToString())
                     .SendAsync("ReceiveNewMessage", messageData);
            }
            return Ok(new
            {
                IsSuccess = true,
                Message = result.Message
            });
        }
    }
}
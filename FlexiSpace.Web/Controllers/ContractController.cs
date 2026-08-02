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

        [HttpPost("{contractId}/start-signing")]
        public async Task<IActionResult> StartContractSigning(long contractId, [FromBody] StartContractSigningRequest request)
        {
            var result = await _contractService.StartContractSigningAsync(contractId, request);
            return HandleResult(result);
        }

        [HttpPost("{contractId}/cancel-signing")]
        public async Task<IActionResult> CancelContractSigning(long contractId)
        {
            var result = await _contractService.CancelContractSigningAsync(contractId);
            return HandleResult(result);
        }

        [HttpPost("{contractId}/cancel")]
        public async Task<IActionResult> CancelContract(long contractId)
        {
            var result = await _contractService.CancelContractAsync(contractId);
            return HandleResult(result);
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

        [HttpGet("GetSnapshotById/{id}")]
        public async Task<IActionResult> GetContractSnapshotById(long id)
        {
            var result = await _contractService.GetContractSnapshotByIdAsync(id);
            return HandleResult(result);
        }

        [HttpGet("{id}/verify-integrity")]
        public async Task<IActionResult> VerifyContractIntegrity(long id)
        {
            var result = await _contractService.VerifyContractIntegrityAsync(id);
            return HandleResult(result);
        }

        [HttpGet("calendar/space/{spaceId}")]
        public async Task<IActionResult> GetSpaceCalendar(long spaceId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var result = await _contractService.GetContractCalendarBySpaceAsync(spaceId, from, to);
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
        public async Task<IActionResult> ValidateContractOtp(long contractId, string inputOtp)
        {
            var result = await _contractService.ContractValidateOtpAsync(contractId, inputOtp);

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

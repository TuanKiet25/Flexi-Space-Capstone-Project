using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS;
using PayOS.Models.Webhooks;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : MyBaseController
    {
        private readonly ITransactionService _transactionService;
        private readonly PayOSClient _payOS;
        public TransactionController(ITransactionService transactionService, PayOSClient payOS)
        {
            _transactionService = transactionService;
            _payOS = payOS;
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionRequest request)
        {
            var result = await _transactionService.CreateTransactionAsync(request);
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            else
            {
                return BadRequest(result.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> VerifyWebhook([FromBody] Webhook webhookData)
        {
            var result = await _transactionService.VerifyWebhookSuccess(webhookData);
            if (result)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}

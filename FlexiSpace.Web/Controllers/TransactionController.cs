using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : MyBaseController
    {
        private readonly ITransactionService _transactionService;
        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
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

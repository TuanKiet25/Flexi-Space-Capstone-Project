using FlexiSpace.Application.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlexiSpace.Web.Controllers
{
    public class TransactionHistoryController : MyBaseController
    {
        private readonly ITransactionHistoryService _transactionHistoryService;

        public TransactionHistoryController(ITransactionHistoryService transactionHistoryService)
        {
            _transactionHistoryService = transactionHistoryService;
        }

        [HttpGet("GetAllTransactionHistoryByUserId")]
        public async Task<IActionResult> GetAllTransactionHistoryByUserId()
        {
            var result = await _transactionHistoryService.GetAllTransactionHistoryByUserId();
            return HandleResult(result);
        }

        [HttpGet("GetTransactionHistoryByUserId/{userId}")]
        public async Task<IActionResult> GetTransactionHistoryByUserId(string userId)
        {
            var result = await _transactionHistoryService.GetTransactionHistoryByUserIdAsync(userId);
            return HandleResult(result);
        }
    }
}

using FlexiSpace.Application.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlexiSpace.Web.Controllers
{
    public class WalletController : MyBaseController
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _walletService.GetAllWallet();
            return HandleResult(result);
        }

        [HttpGet("own")]
        public async Task<IActionResult> GetOwnWallet()
        {
            var result = await _walletService.GetOwnWallet();
            return HandleResult(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetWalletByUserId(string userId)
        {
            var result = await _walletService.GetWalletByUserId(userId);
            return HandleResult(result);
        }

        [HttpPut("spend")]
        public async Task<IActionResult> SpendWalletBalance([FromQuery] decimal spend)
        {
            var result = await _walletService.SpendWalletBalance(spend);
            return HandleResult(result);
        }

        [HttpPut("update-balance/{userId}")]
        public async Task<IActionResult> UpdateWalletBalance(string userId, [FromQuery] decimal uBalance)
        {
            var result = await _walletService.UpdateWalletBalance(userId, uBalance);
            return HandleResult(result);
        }
    }
}

using FlexiSpace.Application.IServices;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAiImageHistoryController : MyBaseController
    {
        private readonly IUserAiImageHistoryService _userAiImageHistoryService;

        public UserAiImageHistoryController(IUserAiImageHistoryService userAiImageHistoryService)
        {
            _userAiImageHistoryService = userAiImageHistoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentUserHistory()
        {
            var result = await _userAiImageHistoryService.GetCurrentUserHistoryAsync();
            return HandleResult(result);
        }

        [HttpGet("{historyId:long}")]
        public async Task<IActionResult> GetByHistoryId(long historyId)
        {
            var result = await _userAiImageHistoryService.GetByHistoryIdAsync(historyId);
            return HandleResult(result);
        }

        [HttpDelete("{historyId:long}")]
        public async Task<IActionResult> HardDelete(long historyId)
        {
            var result = await _userAiImageHistoryService.HardDeleteAsync(historyId);
            return HandleResult(result);
        }
    }
}

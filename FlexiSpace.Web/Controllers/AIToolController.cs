using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIToolController : MyBaseController
    {
        private readonly IAIToolService _aiToolService;

        // Tiêm thẳng AiToolService vào Controller
        public AIToolController(IAIToolService aiToolService)
        {
            _aiToolService = aiToolService;
        }

        [HttpPost("generate-image")]
        public async Task<IActionResult> GenerateImage([FromBody] GenerateAiImageRequest request)
        {
            // Gọi hàm trong Service
            var result = await _aiToolService.GenerateImageAsync(request);
           return HandleResult(result);
        }
    }
}

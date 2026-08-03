using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : MyBaseController
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _reviewService.GetAllAsync();
            return HandleResult(result);
        }

        [HttpGet("GetByUserId/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            var result = await _reviewService.GetByUserIdAsync(userId);
            return HandleResult(result);
        }

        [HttpGet("GetBySpaceId/{spaceId:long}")]
        public async Task<IActionResult> GetBySpaceId(long spaceId)
        {
            var result = await _reviewService.GetBySpaceIdAsync(spaceId);
            return HandleResult(result);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] ReviewRequest request)
        {
            var result = await _reviewService.CreateAsync(request);
            return HandleResult(result);
        }

        [HttpDelete("Delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _reviewService.DeleteAsync(id);
            return HandleResult(result);
        }
    }
}

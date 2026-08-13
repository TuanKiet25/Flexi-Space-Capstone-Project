using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannerController : MyBaseController
    {
        private readonly IBannerService _bannerService;

        public BannerController(IBannerService bannerService)
        {
            _bannerService = bannerService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _bannerService.GetAllAsync();
            return HandleResult(result);
        }

        [HttpGet("GetById/{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _bannerService.GetByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPost("CreateForUser")]
        public async Task<IActionResult> CreateForUser([FromBody] CreateBannerRequest request, int durationInDays, decimal price)
        {
            var result = await _bannerService.CreateForUserAsync(request, durationInDays, price);
            return HandleResult(result);
        }

        [HttpPost("CreateForAdmin")]
        public async Task<IActionResult> CreateForAdmin([FromBody] CreateBannerRequest request, int durationInDays)
        {
            var result = await _bannerService.CreateForAdminAsync(request, durationInDays);
            return HandleResult(result);
        }

        [HttpPut("Update/{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateBannerRequest request)
        {
            var result = await _bannerService.UpdateAsync(id, request);
            return HandleResult(result);
        }

        [HttpDelete("Delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _bannerService.DeleteAsync(id);
            return HandleResult(result);
        }
    }
}

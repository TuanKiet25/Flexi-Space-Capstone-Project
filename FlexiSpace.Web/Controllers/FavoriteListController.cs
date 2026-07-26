using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteListController : MyBaseController
    {
        private readonly IFavoriteListService _favoriteListService;

        public FavoriteListController(IFavoriteListService favoriteListService)
        {
            _favoriteListService = favoriteListService;
        }

        [HttpPost("listings")]
        public async Task<IActionResult> AddListings([FromBody] AddFavoriteListingsRequest request)
        {
            var result = await _favoriteListService.AddListingsAsync(request.ListingIds);
            return HandleResult(result);
        }

        [HttpGet("FavoriteByUser")]
        public async Task<IActionResult> GetFavoriteList()
        {
            var result = await _favoriteListService.GetByUserIdAsync();
            return HandleResult(result);
        }

        [HttpGet("listings/{listingId:long}")]
        public async Task<IActionResult> GetFavoriteListingDetail(long listingId)
        {
            var result = await _favoriteListService.GetListingDetailAsync(listingId);
            return HandleResult(result);
        }

        [HttpDelete("listings/{listingId:long}")]
        public async Task<IActionResult> RemoveListing(long listingId)
        {
            var result = await _favoriteListService.RemoveListingAsync(listingId);
            return HandleResult(result);
        }
    }
}

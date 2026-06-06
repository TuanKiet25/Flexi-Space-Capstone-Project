using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Domain.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListingController : MyBaseController
    {
        private readonly IListingService _listingService;
        public ListingController(IListingService listingService)
        {
            _listingService = listingService;
        }
        [HttpPost("Create")]
        public async Task<IActionResult> CreateListing(ListingRequest listingRequest)
        {
            var result = await _listingService.CreateListingAsync(listingRequest);
            return HandleResult(result);
        }
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllListings([FromQuery] ListingStatusEnum? status)
        {
            var result = await _listingService.GetAllListingsAsync(status);
            return HandleResult(result);
        }
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetListingById(long id)
        {
            var result = await _listingService.GetListingByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateListing(long id, ListingRequest listingRequest)
        {
            var result = await _listingService.UpdateListingAsync(id, listingRequest);
            return HandleResult(result);
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteListing(long id)
        {
            var result = await _listingService.HardDeleteListingAsync(id);
            return HandleResult(result);
        }

        [HttpPatch("Status/{id}")]
        public async Task<IActionResult> AcceptOrCancelListing(long id, ListingStatusRequest request)
        {
            var result = await _listingService.AcceptOrCancelListingAsync(id, request);
            return HandleResult(result);
        }

        [HttpDelete("SoftDelete/{id}")]
        public async Task<IActionResult> SoftDeleteListing(long id)
        {
            var result = await _listingService.SoftDeleteListingAsync(id);
            return HandleResult(result);
        }
    }
}

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
        public async Task<IActionResult> CreateListing(ListingRequest listingRequest, decimal amount, int durationInDays)
        {
            var result = await _listingService.CreateListingAsync(listingRequest, amount, durationInDays);
            return HandleResult(result);
        }
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllListings([FromQuery] ListingStatusEnum? status, [FromQuery] ListingType? listingType, [FromQuery] int? top)
        {
            var result = await _listingService.GetAllListingsAsync(status, listingType, top);
            return HandleResult(result);
        }
        [HttpGet("GetAllByCurrentUser")]
        public async Task<IActionResult> GetListingsByCurrentUser([FromQuery] ListingStatusEnum? status, [FromQuery] ListingType? listingType)
        {
            var result = await _listingService.GetListingsByCurrentUserAsync(status, listingType);
            return HandleResult(result);
        }
        [HttpGet("GetAllByUserId/{userId}")]
        public async Task<IActionResult> GetListingsByUserId(string userId, [FromQuery] ListingStatusEnum? status, [FromQuery] ListingType? listingType)
        {
            var result = await _listingService.GetListingsByUserIdAsync(userId, status, listingType);
            return HandleResult(result);
        }
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetListingById(long id)
        {
            var result = await _listingService.GetListingByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPatch("ViewCount/{id}")]
        public async Task<IActionResult> IncreaseViewCount(long id)
        {
            var result = await _listingService.IncreaseViewCountAsync(id);
            return HandleResult(result);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateListing(long id, ListingRequest listingRequest)
        {
            var result = await _listingService.UpdateListingAsync(id, listingRequest);
            return HandleResult(result);
        }

        [HttpPut("Renew/{id}")]
        public async Task<IActionResult> RenewExpiredListing(long id, ListingRequest listingRequest, decimal amount, int durationInDays)
        {
            var result = await _listingService.RenewExpiredListingAsync(id, listingRequest, amount, durationInDays);
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

        [HttpPost("Reports")]
        public async Task<IActionResult> CreateListingReport([FromBody] CreateListingReportRequest request)
        {
            var result = await _listingService.CreateListingReportAsync(request);
            return HandleResult(result);
        }

        [HttpGet("Reports/{listingId}")]
        public async Task<IActionResult> GetListingReports(long listingId)
        {
            var result = await _listingService.GetListingReportsAsync(listingId);
            return HandleResult(result);
        }

        [HttpGet("Reports/Admin")]
        public async Task<IActionResult> GetReportedListings()
        {
            var result = await _listingService.GetReportedListingsAsync();
            return HandleResult(result);
        }

        [HttpGet("Reports/Admin/Detail/{listingId}")]
        public async Task<IActionResult> GetListingReportDetail(long listingId)
        {
            var result = await _listingService.GetListingReportDetailAsync(listingId);
            return HandleResult(result);
        }

        [HttpDelete("Reports/Admin/{listingId}")]
        public async Task<IActionResult> ClearListingReports(long listingId)
        {
            var result = await _listingService.ClearListingReportsAsync(listingId);
            return HandleResult(result);
        }

        [HttpDelete("SoftDelete/{id}")]
        public async Task<IActionResult> SoftDeleteListing(long id)
        {
            var result = await _listingService.SoftDeleteListingAsync(id);
            return HandleResult(result);
        }

        [HttpGet("SoftDeleted")]
        public async Task<IActionResult> GetSoftDeletedListings([FromQuery] ListingType? listingType)
        {
            var result = await _listingService.GetSoftDeletedListingsAsync(listingType);
            return HandleResult(result);
        }

        [HttpPatch("Restore/{id}")]
        public async Task<IActionResult> RestoreListing(long id)
        {
            var result = await _listingService.RestoreListingAsync(id);
            return HandleResult(result);
        }

        [HttpPost("CreateShareListing")]
        public async Task<IActionResult> CreateShareListing(SharedListingRequest sharedListingRequest, decimal amount, int durationInDays)
        {
            var result = await _listingService.CreateShareListingAsync(sharedListingRequest, amount, durationInDays);
            return HandleResult(result);
        }

        [HttpGet("ShareListing/TimePolicy/{spaceId}")]
        public async Task<IActionResult> GetShareListingTimePolicy(long spaceId)
        {
            var result = await _listingService.GetShareListingTimePolicyAsync(spaceId);
            return HandleResult(result);
        }

        [HttpPut("UpdateShareListing/{id}")]
        public async Task<IActionResult> UpdateShareListing(long id, SharedListingRequest sharedListingRequest)
        {
            var result = await _listingService.UpdateShareListingAsync(id, sharedListingRequest);
            return HandleResult(result);
        }
    }
}

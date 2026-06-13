using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Domain.Enum;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrimaryBookingRequestController : MyBaseController
    {
        private readonly IPrimaryBookingRequestService _primaryBookingRequestService;

        public PrimaryBookingRequestController(IPrimaryBookingRequestService primaryBookingRequestService)
        {
            _primaryBookingRequestService = primaryBookingRequestService;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateBookingRequest([FromBody] BookingRequest request)
        {
            var result = await _primaryBookingRequestService.CreateBookingRequestAsync(request);
            return HandleResult(result);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllBookingRequests([FromQuery] PrimaryBookingRequestStatusEnum? status)
        {
            var result = await _primaryBookingRequestService.GetAllBookingRequestsAsync(status);
            return HandleResult(result);
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetBookingRequestById(long id)
        {
            var result = await _primaryBookingRequestService.GetBookingRequestByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateBookingRequest(long id, [FromBody] BookingRequest request)
        {
            var result = await _primaryBookingRequestService.UpdateBookingRequestAsync(id, request);
            return HandleResult(result);
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteBookingRequest(long id)
        {
            var result = await _primaryBookingRequestService.DeleteBookingRequestAsync(id);
            return HandleResult(result);
        }

        [HttpPatch("Status/{id}")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] BookingStatusRequest request)
        {
            var result = await _primaryBookingRequestService.UpdateStatusAsync(id, request);
            return HandleResult(result);
        }
    }
}

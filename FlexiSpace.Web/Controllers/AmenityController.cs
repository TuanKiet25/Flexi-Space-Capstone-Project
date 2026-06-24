using FlexiSpace.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmenityController : MyBaseController
    {
        private readonly IAmenityService _amenityService;

        public AmenityController(IAmenityService amenityService)
        {
            _amenityService = amenityService;
        }

        [HttpGet("GetAllBySpaceId/{spaceId:long}")]
        public async Task<IActionResult> GetAllAmenitiesBySpaceId(long spaceId)
        {
            var result = await _amenityService.GetAllAmenitiesBySpaceIdAsync(spaceId);
            return HandleResult(result);
        }
    }
}

using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpaceUsageRightController : MyBaseController
    {
        private readonly ISpaceUsageRightService _spaceUsageRightService;

        public SpaceUsageRightController(ISpaceUsageRightService spaceUsageRightService)
        {
            _spaceUsageRightService = spaceUsageRightService;
        }

        [HttpPost("Grant")]
        public async Task<IActionResult> Grant([FromBody] GrantSpaceUsageRightRequest request)
        {
            var result = await _spaceUsageRightService.GrantAsync(request);
            return HandleResult(result);
        }

        [HttpPatch("{id:long}/Permission")]
        public async Task<IActionResult> UpdatePermission(long id, [FromBody] UpdateSpaceUsageRightPermissionRequest request)
        {
            var result = await _spaceUsageRightService.UpdatePermissionAsync(id, request);
            return HandleResult(result);
        }

        [HttpGet("BySpace/{spaceId:long}")]
        public async Task<IActionResult> GetBySpace(long spaceId)
        {
            var result = await _spaceUsageRightService.GetBySpaceAsync(spaceId);
            return HandleResult(result);
        }

        [HttpGet("Mine")]
        public async Task<IActionResult> GetMine([FromQuery] long? spaceId)
        {
            var result = await _spaceUsageRightService.GetMineAsync(spaceId);
            return HandleResult(result);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Revoke(long id)
        {
            var result = await _spaceUsageRightService.RevokeAsync(id);
            return HandleResult(result);
        }
    }
}

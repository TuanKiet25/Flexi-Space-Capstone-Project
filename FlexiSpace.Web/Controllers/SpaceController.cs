using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests.Space;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpaceController : MyBaseController
    {
        private readonly ISpaceService _spaceService;
        public SpaceController(ISpaceService spaceService)
        {
            _spaceService = spaceService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllSpaces([FromQuery] FilterGetAllSpace filter)
        {
            var result = await _spaceService.GetAll(filter);
            return HandleResult(result);
        }

        [HttpGet("GetAddress")]
        public async Task<IActionResult> GetAddress([FromQuery] string? provinceCode, [FromQuery] string? districtCode)
        {
            var result = await _spaceService.GetAddress(provinceCode, districtCode);
            return HandleResult(result);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateSpace([FromForm] CreateSpaceRQ space)
        {
            var result = await _spaceService.Create(space);
            return HandleResult(result);
        }

        [HttpGet("GetById{id:long}")]
        public async Task<IActionResult> GetSpaceById(long id)
        {
            var result = await _spaceService.GetById(id);
            return HandleResult(result);
        }

        [HttpPut("Update{id:long}")]
        public async Task<IActionResult> UpdateSpace(long id, [FromBody] CreateSpaceRQ space)
        {
            var result = await _spaceService.Update(id, space);
            return HandleResult(result);
        }

        [HttpDelete("Delete{id:long}")]
        public async Task<IActionResult> DeleteSpace(long id)
        {
            var result = await _spaceService.Delete(id);
            return HandleResult(result);
        }
    }
}

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

        [HttpPost("Create")]
        public async Task<IActionResult> CreateSpace([FromBody] CreateSpaceRQ space)
        {
            var result = await _spaceService.Create(space);
            return HandleResult(result);
        }
    }
}

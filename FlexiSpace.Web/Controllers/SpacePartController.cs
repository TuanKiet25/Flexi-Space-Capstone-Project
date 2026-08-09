using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests.Space;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpacePartController : MyBaseController
    {
        private readonly ISpacePartService _spacePartService;

        public SpacePartController(ISpacePartService spacePartService)
        {
            _spacePartService = spacePartService;
        }

        [HttpPost("Create/{parentSpaceId:long}")]
        public async Task<IActionResult> Create(long parentSpaceId, [FromBody] CreateSpacePartRQ request)
        {
            var result = await _spacePartService.CreateAsync(parentSpaceId, request);
            return HandleResult(result);
        }

        [HttpPost("CreateMany/{parentSpaceId:long}")]
        public async Task<IActionResult> CreateMany(long parentSpaceId, [FromBody] CreateSpacePartsRQ request)
        {
            var result = await _spacePartService.CreateManyAsync(parentSpaceId, request);
            return HandleResult(result);
        }

        [HttpGet("GetByParent/{parentSpaceId:long}")]
        public async Task<IActionResult> GetByParent(long parentSpaceId)
        {
            var result = await _spacePartService.GetByParentSpaceAsync(parentSpaceId);
            return HandleResult(result);
        }

        [HttpGet("GetById/{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _spacePartService.GetByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPut("Update/{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateSpacePartRQ request)
        {
            var result = await _spacePartService.UpdateAsync(id, request);
            return HandleResult(result);
        }

        [HttpDelete("Delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _spacePartService.DeleteAsync(id);
            return HandleResult(result);
        }
    }
}

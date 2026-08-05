using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests.PriorityLevelRQ;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriorityLevelController : MyBaseController
    {
        private readonly IPriorityLevelService _priorityLevelService;

        public PriorityLevelController(IPriorityLevelService priorityLevelService)
        {
            _priorityLevelService = priorityLevelService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll([FromQuery] FilterGetAllPriorityLevel filter)
        {
            var result = await _priorityLevelService.GetAll(filter);
            return HandleResult(result);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreatePriorityLevel priorityLevel)
        {
            var result = await _priorityLevelService.Create(priorityLevel);
            return HandleResult(result);
        }

        [HttpGet("GetById{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _priorityLevelService.GetById(id);
            return HandleResult(result);
        }

        [HttpPut("Update{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreatePriorityLevel priorityLevel)
        {
            var result = await _priorityLevelService.Update(id, priorityLevel);
            return HandleResult(result);
        }

        [HttpDelete("Delete{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _priorityLevelService.Delete(id);
            return HandleResult(result);
        }
    }
}

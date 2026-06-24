using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests.BussinessCategoryRQ;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BussinessCategoryController : MyBaseController
    {
        private readonly IBussinessCategoryService _bussinessCategoryService;

        public BussinessCategoryController(IBussinessCategoryService bussinessCategoryService)
        {
            _bussinessCategoryService = bussinessCategoryService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll([FromQuery] FilterGetAllBussinessCategory filter)
        {
            var result = await _bussinessCategoryService.GetAll(filter);
            return HandleResult(result);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateBussinessCategory bussinessCategory)
        {
            var result = await _bussinessCategoryService.Create(bussinessCategory);
            return HandleResult(result);
        }

        [HttpPost("CreateList")]
        public async Task<IActionResult> CreateList([FromBody] CreateBussinessCategories bussinessCategories)
        {
            var result = await _bussinessCategoryService.CreateList(bussinessCategories);
            return HandleResult(result);
        }

        [HttpGet("GetById{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _bussinessCategoryService.GetById(id);
            return HandleResult(result);
        }

        [HttpPut("Update{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateBussinessCategory bussinessCategory)
        {
            var result = await _bussinessCategoryService.Update(id, bussinessCategory);
            return HandleResult(result);
        }

        [HttpDelete("Delete{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _bussinessCategoryService.Delete(id);
            return HandleResult(result);
        }
    }
}

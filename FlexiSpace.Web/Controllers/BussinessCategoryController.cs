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

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateBussinessCategory bussinessCategory)
        {
            var result = await _bussinessCategoryService.Create(bussinessCategory);
            return HandleResult(result);
        }
    }
}

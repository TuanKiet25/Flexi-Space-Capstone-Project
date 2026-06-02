using FlexiSpace.Application.ViewModels.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MyBaseController : ControllerBase
    {
        protected IActionResult HandleResult<T>(ServiceResult<T> result)
        {
            if (result == null) return NotFound();
            if (result.IsNotFound) return NotFound(result.Message);
            if (result.IsSuccess)
            {
                if (result.Data != null) return Ok(result.Data);
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }
    }
}

using FlexiSpace.Application.IServices;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class PictureController : MyBaseController
    {
        private readonly IPictureURL _pictureURLService;

        public PictureController(IPictureURL pictureURLService)
        {
            _pictureURLService = pictureURLService;
        }

        [HttpPost]
        public async Task<IActionResult> UploadPicture([FromForm] List<IFormFile> file, [FromForm] long? spaceId)
        {
            if (file == null || file.Count == 0)
                return BadRequest("No files uploaded.");

            var pictureURL = await _pictureURLService.UploadImagesAsync(file, spaceId);
            return Ok(pictureURL);
        }
    }
}

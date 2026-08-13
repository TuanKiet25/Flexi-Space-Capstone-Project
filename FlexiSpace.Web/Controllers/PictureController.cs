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
        public async Task<IActionResult> UploadPicture([FromForm] List<IFormFile> file, [FromForm] long? spaceId, [FromForm] string? userProfileId, [FromForm] long? listingId, [FromForm] long? contractId, [FromForm] long? bannerId )
        {
            if (file == null || file.Count == 0)
                return BadRequest("No files uploaded.");

            var pictureURL = await _pictureURLService.UploadImagesAsync(file, spaceId, userProfileId, listingId, contractId, bannerId);
            return Ok(pictureURL);
        }

        [HttpDelete("{*publicId}")]
        public async Task<IActionResult> DeletePicture(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return BadRequest("PublicId is required.");

            var result = await _pictureURLService.DeleteImageFromCloudAsync(publicId);
            if (result)
                return Ok("Deleted successfully.");

            return BadRequest("Failed to delete picture.");
        }
    }
}

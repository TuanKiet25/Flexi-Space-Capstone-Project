using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    public class ProfileController : MyBaseController
    {
        private readonly IProfileService _profileService;
        private readonly ICurrentUserService _currentUserService;

        public ProfileController(IProfileService profileService, ICurrentUserService currentUserService)
        {
            _profileService = profileService;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate([FromBody] ProfileRequest request)
        {
            var result = await _profileService.CreateOrUpdateProfileAsync(request);
            return HandleResult(result);
        }


        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUserService.UserId;
            var result = await _profileService.GetProfileByUserIdAsync(userId ?? string.Empty);
            return HandleResult(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            var result = await _profileService.GetProfileByUserIdAsync(userId);
            return HandleResult(result);
        }
    }
}

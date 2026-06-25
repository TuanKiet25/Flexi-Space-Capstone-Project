using FlexiSpace.Application.IServices;
using FlexiSpace.Domain.Enum;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    public class UserController : MyBaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _userService.GetAllUsersAsync();
            return HandleResult(result);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            var result = await _userService.GetUserByUserIdAsync(userId);
            return HandleResult(result);
        }

        [HttpPut("status/{userId}")]
        public async Task<IActionResult> ChangeStatus(string userId, [FromQuery] UserStatus status)
        {
            var result = await _userService.ChangeUserStatusAsync(userId, status);
            return HandleResult(result);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> Delete(string userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            return HandleResult(result);
        }
    }
}

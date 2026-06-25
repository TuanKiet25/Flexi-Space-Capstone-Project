using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Enum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IUserService
    {
        Task<ServiceResult<IEnumerable<UserResponse>>> GetAllUsersAsync();
        Task<ServiceResult<UserResponse>> GetUserByUserIdAsync(string userId);
        Task<ServiceResult<UserResponse>> ChangeUserStatusAsync(string userId, UserStatus status);
        Task<ServiceResult<string>> DeleteUserAsync(string userId);
    }
}

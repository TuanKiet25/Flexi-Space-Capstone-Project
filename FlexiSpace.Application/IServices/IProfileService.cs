using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IProfileService
    {
        Task<ServiceResult<ProfileResponse>> CreateOrUpdateProfileAsync(ProfileRequest request);
        Task<ServiceResult<ProfileResponse>> GetProfileByUserIdAsync(string userId);
        Task<ServiceResult<ProfileResponse>> VerifyProfileAsync(string userProfileId, VerifyProfileRequest request);
    }
}

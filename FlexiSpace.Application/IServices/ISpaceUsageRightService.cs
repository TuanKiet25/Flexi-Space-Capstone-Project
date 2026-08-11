using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;

namespace FlexiSpace.Application.IServices
{
    public interface ISpaceUsageRightService
    {
        Task<ServiceResult<SpaceUsageRightResponse>> GrantAsync(GrantSpaceUsageRightRequest request);
        Task<ServiceResult<SpaceUsageRightResponse>> UpdatePermissionAsync(long id, UpdateSpaceUsageRightPermissionRequest request);
        Task<ServiceResult<IEnumerable<SpaceUsageRightResponse>>> GetBySpaceAsync(long spaceId);
        Task<ServiceResult<IEnumerable<SpaceUsageRightResponse>>> GetMineAsync(long? spaceId = null);
        Task<ServiceResult<string>> RevokeAsync(long id);
    }
}

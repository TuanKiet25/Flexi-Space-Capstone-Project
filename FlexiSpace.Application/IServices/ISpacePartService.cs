using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Application.ViewModels.Responses.Space;

namespace FlexiSpace.Application.IServices
{
    public interface ISpacePartService
    {
        Task<ServiceResult<SpacePartResponse>> CreateAsync(long parentSpaceId, CreateSpacePartRQ request);
        Task<ServiceResult<IEnumerable<SpacePartResponse>>> CreateManyAsync(long parentSpaceId, CreateSpacePartsRQ request);
        Task<ServiceResult<IEnumerable<SpacePartResponse>>> GetByParentSpaceAsync(long parentSpaceId);
        Task<ServiceResult<SpacePartResponse>> GetByIdAsync(long id);
        Task<ServiceResult<SpacePartResponse>> UpdateAsync(long id, UpdateSpacePartRQ request);
        Task<ServiceResult<string>> DeleteAsync(long id);
    }
}

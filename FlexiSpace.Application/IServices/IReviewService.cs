using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IReviewService
    {
        Task<ServiceResult<IEnumerable<ReviewResponse>>> GetAllAsync();
        Task<ServiceResult<IEnumerable<ReviewResponse>>> GetByUserIdAsync(string userId);
        Task<ServiceResult<IEnumerable<ReviewResponse>>> GetBySpaceIdAsync(long spaceId);
        Task<ServiceResult<ReviewResponse>> CreateAsync(ReviewRequest request);
        Task<ServiceResult<bool>> DeleteAsync(long id);
    }
}

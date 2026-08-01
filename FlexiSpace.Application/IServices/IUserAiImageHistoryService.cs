using FlexiSpace.Application.ViewModels.Responses;

namespace FlexiSpace.Application.IServices
{
    public interface IUserAiImageHistoryService
    {
        Task<ServiceResult<List<UserAiImageHistoryResponse>>> GetCurrentUserHistoryAsync();
    }
}

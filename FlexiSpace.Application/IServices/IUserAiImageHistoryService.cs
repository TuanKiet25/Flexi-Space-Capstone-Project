using FlexiSpace.Application.ViewModels.Responses;

namespace FlexiSpace.Application.IServices
{
    public interface IUserAiImageHistoryService
    {
        Task<ServiceResult<List<UserAiImageHistoryResponse>>> GetCurrentUserHistoryAsync();
        Task<ServiceResult<UserAiImageHistoryResponse>> GetByHistoryIdAsync(long historyId);
        Task<ServiceResult<UserAiImageHistoryResponse>> HardDeleteAsync(long historyId);
    }
}

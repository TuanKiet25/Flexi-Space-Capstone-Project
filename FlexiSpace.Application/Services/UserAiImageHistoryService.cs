using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;

namespace FlexiSpace.Application.Services
{
    public class UserAiImageHistoryService : IUserAiImageHistoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UserAiImageHistoryService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<List<UserAiImageHistoryResponse>>> GetCurrentUserHistoryAsync()
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Failure("UserId la bat buoc.");
            }

            var histories = await _unitOfWork.userAiImageHistoryRepository.GetAllAsync(x => x.UserId == userId);
            var response = histories
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new UserAiImageHistoryResponse
                {
                    Id = x.Id,
                    Prompt = x.Prompt,
                    ResultImageUrl = x.ResultImageUrl,
                    CreatedAt = x.CreatedAt
                })
                .ToList();

            return Success(response);
        }

        private static ServiceResult<List<UserAiImageHistoryResponse>> Success(List<UserAiImageHistoryResponse> data) =>
            new() { IsSuccess = true, Data = data };

        private static ServiceResult<List<UserAiImageHistoryResponse>> Failure(string message) =>
            new() { IsSuccess = false, Message = message };
    }
}

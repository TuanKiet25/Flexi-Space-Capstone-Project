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

        public async Task<ServiceResult<UserAiImageHistoryResponse>> GetByHistoryIdAsync(long historyId)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Failure<UserAiImageHistoryResponse>("UserId la bat buoc.");
            }

            var history = await _unitOfWork.userAiImageHistoryRepository.GetAsync(
                x => x.Id == historyId && x.UserId == userId);

            if (history == null)
            {
                return NotFound<UserAiImageHistoryResponse>("Khong tim thay lich su AI image voi Id da cho.");
            }

            return Success(MapToResponse(history));
        }

        public async Task<ServiceResult<UserAiImageHistoryResponse>> HardDeleteAsync(long historyId)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Failure<UserAiImageHistoryResponse>("UserId la bat buoc.");
            }

            var history = await _unitOfWork.userAiImageHistoryRepository.GetAsync(
                x => x.Id == historyId && x.UserId == userId);

            if (history == null)
            {
                return NotFound<UserAiImageHistoryResponse>("Khong tim thay lich su AI image voi Id da cho.");
            }

            await _unitOfWork.userAiImageHistoryRepository.RemoveByIdAsync(historyId);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult<UserAiImageHistoryResponse>
            {
                IsSuccess = true,
                Message = "Xoa lich su AI image thanh cong."
            };
        }

        private static ServiceResult<List<UserAiImageHistoryResponse>> Success(List<UserAiImageHistoryResponse> data) =>
            new() { IsSuccess = true, Data = data };

        private static ServiceResult<List<UserAiImageHistoryResponse>> Failure(string message) =>
            new() { IsSuccess = false, Message = message };

        private static ServiceResult<T> Success<T>(T data) =>
            new() { IsSuccess = true, Data = data };

        private static ServiceResult<T> Failure<T>(string message) =>
            new() { IsSuccess = false, Message = message };

        private static ServiceResult<T> NotFound<T>(string message) =>
            new() { IsSuccess = false, IsNotFound = true, Message = message };

        private static UserAiImageHistoryResponse MapToResponse(Domain.Entities.UserAiImageHistory history) =>
            new()
            {
                Id = history.Id,
                Prompt = history.Prompt,
                ResultImageUrl = history.ResultImageUrl,
                CreatedAt = history.CreatedAt
            };
    }
}

using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Application.Services
{
    public class NotificationExpoService : INotificationExpoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public NotificationExpoService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<string>> SaveToken(SaveTokenRequest saveTokenRequest)
        {
            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return new ServiceResult<string>
                {
                    IsSuccess = false,
                    Message = "User is not authenticated."
                };
            }

            if (string.IsNullOrWhiteSpace(saveTokenRequest.Token))
            {
                return new ServiceResult<string>
                {
                    IsSuccess = false,
                    Message = "Token is required."
                };
            }

            var existingToken = await _unitOfWork.deviceTokenRepository.GetAsync(
                t => t.UserId == currentUserId && t.ExpoPushToken == saveTokenRequest.Token);

            if (existingToken == null)
            {
                var newToken = new DeviceToken
                {
                    UserId = currentUserId,
                    ExpoPushToken = saveTokenRequest.Token,
                    Platform = saveTokenRequest.Platform ?? string.Empty
                };

                await _unitOfWork.deviceTokenRepository.AddAsync(newToken);
            }
            else
            {
                existingToken.UpdatedAt = DateTime.UtcNow;
                existingToken.Platform = saveTokenRequest.Platform ?? existingToken.Platform;
                await _unitOfWork.deviceTokenRepository.UpdateAsync(existingToken);
            }

            await _unitOfWork.SaveChangesAsync();
            return new ServiceResult<string>
            {
                IsSuccess = true,
                Data = "Token saved",
                Message = "Token saved"
            };
        }
    }
}

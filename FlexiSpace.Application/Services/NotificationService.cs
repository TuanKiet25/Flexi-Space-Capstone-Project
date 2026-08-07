using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;

namespace FlexiSpace.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationRealtimeSender _realtimeSender;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public NotificationService(
            IUnitOfWork unitOfWork,
            INotificationRealtimeSender realtimeSender,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _realtimeSender = realtimeSender;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<NotificationResponse> CreateAsync(string userId, string title, string content,
            NotificationTypeEnum type, string? referenceId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId không được để trống.", nameof(userId));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Tiêu đề thông báo không được để trống.", nameof(title));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung thông báo không được để trống.", nameof(content));

            var notification = new Notification
            {
                UserId = userId,
                Title = title.Trim(),
                Content = content.Trim(),
                Type = type,
                ReferenceId = string.IsNullOrWhiteSpace(referenceId) ? null! : referenceId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.notificationRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<NotificationResponse>(notification);

            await _realtimeSender.SendToUserAsync(userId, response);
            return response;
        }

        public async Task<ServiceResult<List<NotificationResponse>>> GetHistoryByCurrentUserAsync()
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return new ServiceResult<List<NotificationResponse>>
                {
                    IsSuccess = false,
                    Message = "User is not authenticated."
                };
            }

            var notifications = await _unitOfWork.notificationRepository.GetAllWithSortAsync(
                n => n.UserId == currentUserId,
                orderBy: query => query.OrderByDescending(n => n.CreatedAt));

            return new ServiceResult<List<NotificationResponse>>
            {
                IsSuccess = true,
                Data = _mapper.Map<List<NotificationResponse>>(notifications)
            };
        }

        public async Task<ServiceResult<int>> GetUnreadCountByCurrentUserAsync()
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return new ServiceResult<int>
                {
                    IsSuccess = false,
                    Message = "User is not authenticated."
                };
            }

            var notifications = await _unitOfWork.notificationRepository.GetAllAsync(
                n => n.UserId == currentUserId && !n.IsRead);

            return new ServiceResult<int>
            {
                IsSuccess = true,
                Data = notifications.Count
            };
        }

        public async Task<ServiceResult<NotificationResponse>> MarkAsReadAsync(long notificationId)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return new ServiceResult<NotificationResponse>
                {
                    IsSuccess = false,
                    Message = "User is not authenticated."
                };
            }

            var notification = await _unitOfWork.notificationRepository.GetAsync(
                n => n.Id == notificationId && n.UserId == currentUserId);

            if (notification == null)
            {
                return new ServiceResult<NotificationResponse>
                {
                    IsSuccess = false,
                    IsNotFound = true,
                    Message = "Notification not found."
                };
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.notificationRepository.UpdateAsync(notification);
                await _unitOfWork.SaveChangesAsync();
            }

            return new ServiceResult<NotificationResponse>
            {
                IsSuccess = true,
                Data = _mapper.Map<NotificationResponse>(notification)
            };
        }

        public async Task<ServiceResult<int>> MarkAllAsReadByCurrentUserAsync()
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return new ServiceResult<int>
                {
                    IsSuccess = false,
                    Message = "User is not authenticated."
                };
            }

            var unreadNotifications = await _unitOfWork.notificationRepository.GetAllAsync(
                n => n.UserId == currentUserId && !n.IsRead);

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.notificationRepository.UpdateAsync(notification);
            }

            if (unreadNotifications.Count > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            return new ServiceResult<int>
            {
                IsSuccess = true,
                Data = unreadNotifications.Count
            };
        }
    }
}

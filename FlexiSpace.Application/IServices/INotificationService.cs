using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Enum;

namespace FlexiSpace.Application.IServices
{
    public interface INotificationService
    {
        Task<NotificationResponse> CreateAsync(string userId, string title, string content,
            NotificationTypeEnum type, string? referenceId = null);
    }
}

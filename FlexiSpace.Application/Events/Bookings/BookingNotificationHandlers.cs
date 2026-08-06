using FlexiSpace.Application.IServices;
using FlexiSpace.Domain.Enum;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlexiSpace.Application.Events.Bookings
{
    public sealed class BookingRequestCreatedNotificationHandler : INotificationHandler<BookingRequestCreatedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<BookingRequestCreatedNotificationHandler> _logger;

        public BookingRequestCreatedNotificationHandler(
            INotificationService notificationService,
            ILogger<BookingRequestCreatedNotificationHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(BookingRequestCreatedEvent notification, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(notification.LessorId))
            {
                _logger.LogWarning("Skip booking request notification because lessor id is empty. BookingRequestId: {BookingRequestId}", notification.BookingRequestId);
                return;
            }

            try
            {
                var spaceName = string.IsNullOrWhiteSpace(notification.SpaceAddress)
                    ? $"space #{notification.SpaceId}"
                    : notification.SpaceAddress;

                await _notificationService.CreateAsync(
                    notification.LessorId,
                    "Yeu cau dat cho moi",
                    $"Co nguoi vua gui yeu cau dat cho cho {spaceName}.",
                    NotificationTypeEnum.Booking,
                    notification.BookingRequestId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send booking request created notification. BookingRequestId: {BookingRequestId}", notification.BookingRequestId);
            }
        }
    }

    public sealed class BookingRequestApprovedNotificationHandler : INotificationHandler<BookingRequestApprovedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<BookingRequestApprovedNotificationHandler> _logger;

        public BookingRequestApprovedNotificationHandler(
            INotificationService notificationService,
            ILogger<BookingRequestApprovedNotificationHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(BookingRequestApprovedEvent notification, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(notification.LesseeId))
            {
                _logger.LogWarning("Skip booking request approved notification because lessee id is empty. BookingRequestId: {BookingRequestId}", notification.BookingRequestId);
                return;
            }

            try
            {
                var spaceName = string.IsNullOrWhiteSpace(notification.SpaceAddress)
                    ? $"space #{notification.SpaceId}"
                    : notification.SpaceAddress;

                await _notificationService.CreateAsync(
                    notification.LesseeId,
                    "Yeu cau dat cho da duoc duyet",
                    $"Yeu cau dat cho cua ban cho {spaceName} da duoc nguoi cho thue chap thuan.",
                    NotificationTypeEnum.Booking,
                    notification.BookingRequestId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send booking request approved notification. BookingRequestId: {BookingRequestId}", notification.BookingRequestId);
            }
        }
    }
}

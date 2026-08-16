using FluentAssertions;
using FlexiSpace.Application.Events.Bookings;
using FlexiSpace.Application.IServices;
using FlexiSpace.Domain.Enum;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlexiSpace.Application.Tests
{
    public class BookingNotificationHandlerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;

        public BookingNotificationHandlerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
        }

        [Fact]
        public void BookingRequestCreatedEvent_ConstructedWithValues_ExposesProperties()
        {
            // 1. ARRANGE
            var notification = new BookingRequestCreatedEvent(1, 2, 3, "lessee-1", "lessor-1", "Space A");

            // 2. ACT
            var result = notification with { SpaceAddress = "Space B" };

            // 3. ASSERT
            notification.BookingRequestId.Should().Be(1);
            notification.ListingId.Should().Be(2);
            notification.SpaceId.Should().Be(3);
            notification.LesseeId.Should().Be("lessee-1");
            notification.LessorId.Should().Be("lessor-1");
            notification.SpaceAddress.Should().Be("Space A");
            result.SpaceAddress.Should().Be("Space B");
        }

        [Fact]
        public void BookingRequestApprovedEvent_ConstructedWithValues_ExposesProperties()
        {
            // 1. ARRANGE
            var notification = new BookingRequestApprovedEvent(1, 2, 3, "lessor-1", "lessee-1", "Space A");

            // 2. ACT
            var result = notification with { SpaceAddress = "Space B" };

            // 3. ASSERT
            notification.BookingRequestId.Should().Be(1);
            notification.ListingId.Should().Be(2);
            notification.SpaceId.Should().Be(3);
            notification.LessorId.Should().Be("lessor-1");
            notification.LesseeId.Should().Be("lessee-1");
            notification.SpaceAddress.Should().Be("Space A");
            result.SpaceAddress.Should().Be("Space B");
        }

        [Fact]
        public async Task Handle_CreatedEventWithEmptyLessorId_DoesNotCreateNotification()
        {
            // 1. ARRANGE
            var handler = new BookingRequestCreatedNotificationHandler(
                _mockNotificationService.Object,
                Mock.Of<ILogger<BookingRequestCreatedNotificationHandler>>());
            var notification = new BookingRequestCreatedEvent(1, 2, 3, "lessee-1", " ", "Space A");

            // 2. ACT
            await handler.Handle(notification, CancellationToken.None);

            // 3. ASSERT
            _mockNotificationService.Verify(s => s.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationTypeEnum>(),
                It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CreatedEventWithValidLessorId_CreatesBookingNotification()
        {
            // 1. ARRANGE
            var handler = new BookingRequestCreatedNotificationHandler(
                _mockNotificationService.Object,
                Mock.Of<ILogger<BookingRequestCreatedNotificationHandler>>());
            var notification = new BookingRequestCreatedEvent(10, 20, 30, "lessee-1", "lessor-1", "Space A");

            _mockNotificationService
                .Setup(s => s.CreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<NotificationTypeEnum>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(new ViewModels.Responses.NotificationResponse());

            // 2. ACT
            await handler.Handle(notification, CancellationToken.None);

            // 3. ASSERT
            _mockNotificationService.Verify(s => s.CreateAsync(
                "lessor-1",
                "Yeu cau dat cho moi",
                "Co nguoi vua gui yeu cau dat cho cho Space A.",
                NotificationTypeEnum.Booking,
                "10"), Times.Once);
        }

        [Fact]
        public async Task Handle_CreatedEventWhenNotificationThrows_SwallowsException()
        {
            // 1. ARRANGE
            var handler = new BookingRequestCreatedNotificationHandler(
                _mockNotificationService.Object,
                Mock.Of<ILogger<BookingRequestCreatedNotificationHandler>>());
            var notification = new BookingRequestCreatedEvent(10, 20, 30, "lessee-1", "lessor-1", null);

            _mockNotificationService
                .Setup(s => s.CreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<NotificationTypeEnum>(),
                    It.IsAny<string?>()))
                .ThrowsAsync(new InvalidOperationException("notification failed"));

            // 2. ACT
            var act = async () => await handler.Handle(notification, CancellationToken.None);

            // 3. ASSERT
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Handle_ApprovedEventWithEmptyLesseeId_DoesNotCreateNotification()
        {
            // 1. ARRANGE
            var handler = new BookingRequestApprovedNotificationHandler(
                _mockNotificationService.Object,
                Mock.Of<ILogger<BookingRequestApprovedNotificationHandler>>());
            var notification = new BookingRequestApprovedEvent(1, 2, 3, "lessor-1", "", "Space A");

            // 2. ACT
            await handler.Handle(notification, CancellationToken.None);

            // 3. ASSERT
            _mockNotificationService.Verify(s => s.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationTypeEnum>(),
                It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ApprovedEventWithValidLesseeId_CreatesBookingNotification()
        {
            // 1. ARRANGE
            var handler = new BookingRequestApprovedNotificationHandler(
                _mockNotificationService.Object,
                Mock.Of<ILogger<BookingRequestApprovedNotificationHandler>>());
            var notification = new BookingRequestApprovedEvent(10, 20, 30, "lessor-1", "lessee-1", null);

            _mockNotificationService
                .Setup(s => s.CreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<NotificationTypeEnum>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(new ViewModels.Responses.NotificationResponse());

            // 2. ACT
            await handler.Handle(notification, CancellationToken.None);

            // 3. ASSERT
            _mockNotificationService.Verify(s => s.CreateAsync(
                "lessee-1",
                "Yeu cau dat cho da duoc duyet",
                "Yeu cau dat cho cua ban cho space #30 da duoc nguoi cho thue chap thuan.",
                NotificationTypeEnum.Booking,
                "10"), Times.Once);
        }
    }
}

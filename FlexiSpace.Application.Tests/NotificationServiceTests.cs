using AutoMapper;
using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Moq;
using System;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class NotificationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<INotificationRepository> _mockNotificationRepository;
        private readonly Mock<INotificationRealtimeSender> _mockRealtimeSender;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly NotificationService _sut;

        public NotificationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _mockRealtimeSender = new Mock<INotificationRealtimeSender>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork.SetupGet(u => u.notificationRepository).Returns(_mockNotificationRepository.Object);

            _sut = new NotificationService(
                _mockUnitOfWork.Object,
                _mockRealtimeSender.Object,
                _mockCurrentUserService.Object,
                _mockMapper.Object);
        }

        [Theory]
        [InlineData("", "Title", "Content", "userId")]
        [InlineData("user-1", "", "Content", "title")]
        [InlineData("user-1", "Title", "", "content")]
        public async Task CreateAsync_MissingRequiredValue_ThrowsArgumentException(
            string userId,
            string title,
            string content,
            string parameterName)
        {
            // 1. ARRANGE

            // 2. ACT
            var act = async () => await _sut.CreateAsync(userId, title, content, NotificationTypeEnum.Booking);

            // 3. ASSERT
            await act.Should().ThrowAsync<ArgumentException>()
                .Where(ex => ex.ParamName == parameterName);
            _mockNotificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ValidInput_PersistsAndSendsRealtimeNotification()
        {
            // 1. ARRANGE
            var response = new NotificationResponse
            {
                UserId = "user-1",
                Title = "Booking updated",
                Content = "Your booking was approved",
                Type = NotificationTypeEnum.Booking
            };
            _mockNotificationRepository
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<NotificationResponse>(It.IsAny<Notification>()))
                .Returns(response);
            _mockRealtimeSender
                .Setup(s => s.SendToUserAsync("user-1", response))
                .Returns(Task.CompletedTask);

            // 2. ACT
            var result = await _sut.CreateAsync("user-1", " Booking updated ", " Your booking was approved ", NotificationTypeEnum.Booking, "booking-1");

            // 3. ASSERT
            result.Should().Be(response);
            _mockNotificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.UserId == "user-1" &&
                n.Title == "Booking updated" &&
                n.Content == "Your booking was approved" &&
                n.Type == NotificationTypeEnum.Booking &&
                n.ReferenceId == "booking-1" &&
                !n.IsRead &&
                n.IsActive)), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockRealtimeSender.Verify(s => s.SendToUserAsync("user-1", response), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_BlankReferenceId_PersistsNullReferenceId()
        {
            // 1. ARRANGE
            var response = new NotificationResponse();
            _mockNotificationRepository
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<NotificationResponse>(It.IsAny<Notification>()))
                .Returns(response);
            _mockRealtimeSender
                .Setup(s => s.SendToUserAsync(It.IsAny<string>(), It.IsAny<NotificationResponse>()))
                .Returns(Task.CompletedTask);

            // 2. ACT
            await _sut.CreateAsync("user-1", "Title", "Content", NotificationTypeEnum.System, " ");

            // 3. ASSERT
            _mockNotificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n => n.ReferenceId == null)), Times.Once);
        }
    }
}

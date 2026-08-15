using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Domain.Entities;
using Moq;
using System.Linq.Expressions;

namespace FlexiSpace.Application.Tests
{
    public class NotificationExpoServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IDeviceTokenRepository> _mockDeviceTokenRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly NotificationExpoService _sut;

        public NotificationExpoServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockDeviceTokenRepository = new Mock<IDeviceTokenRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.SetupGet(u => u.deviceTokenRepository).Returns(_mockDeviceTokenRepository.Object);

            _sut = new NotificationExpoService(_mockUnitOfWork.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task SaveToken_UnauthenticatedUser_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns((string?)null);
            var request = new SaveTokenRequest { Token = "ExpoToken", Platform = "ios" };

            // 2. ACT
            var result = await _sut.SaveToken(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("User is not authenticated.");
            _mockDeviceTokenRepository.Verify(r => r.AddAsync(It.IsAny<DeviceToken>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SaveToken_MissingToken_ReturnsFailedResult(string token)
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            var request = new SaveTokenRequest { Token = token, Platform = "ios" };

            // 2. ACT
            var result = await _sut.SaveToken(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Token is required.");
            _mockDeviceTokenRepository.Verify(r => r.AddAsync(It.IsAny<DeviceToken>()), Times.Never);
        }

        [Fact]
        public async Task SaveToken_NewToken_AddsTokenAndSavesChanges()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockDeviceTokenRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<DeviceToken, bool>>>()))
                .ReturnsAsync((DeviceToken)null!);
            _mockDeviceTokenRepository
                .Setup(r => r.AddAsync(It.IsAny<DeviceToken>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            var request = new SaveTokenRequest { Token = "ExpoToken", Platform = "android" };

            // 2. ACT
            var result = await _sut.SaveToken(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Token saved");
            result.Message.Should().Be("Token saved");
            _mockDeviceTokenRepository.Verify(r => r.AddAsync(It.Is<DeviceToken>(t =>
                t.UserId == "user-1" &&
                t.ExpoPushToken == "ExpoToken" &&
                t.Platform == "android")), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveToken_ExistingToken_UpdatesTokenAndSavesChanges()
        {
            // 1. ARRANGE
            var existingToken = new DeviceToken
            {
                Id = "token-1",
                UserId = "user-1",
                ExpoPushToken = "ExpoToken",
                Platform = "ios",
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockDeviceTokenRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<DeviceToken, bool>>>()))
                .ReturnsAsync(existingToken);
            _mockDeviceTokenRepository
                .Setup(r => r.UpdateAsync(existingToken))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            var request = new SaveTokenRequest { Token = "ExpoToken", Platform = "android" };

            // 2. ACT
            var result = await _sut.SaveToken(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            existingToken.Platform.Should().Be("android");
            existingToken.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            _mockDeviceTokenRepository.Verify(r => r.UpdateAsync(existingToken), Times.Once);
            _mockDeviceTokenRepository.Verify(r => r.AddAsync(It.IsAny<DeviceToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}

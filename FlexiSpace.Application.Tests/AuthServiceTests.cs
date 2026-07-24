using BCrypt.Net;
using FluentAssertions;
using FlexiSpace.Application;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IJwtProvider> _mockJwtProvider;
        private readonly Mock<ITurnstileService> _mockTurnstileService;
        private readonly IDistributedCache _cache;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockJwtProvider = new Mock<IJwtProvider>();
            _mockTurnstileService = new Mock<ITurnstileService>();
            _cache = new TestDistributedCache();

            _mockUnitOfWork.SetupGet(u => u.userRepository).Returns(_mockUserRepository.Object);

            _sut = new AuthService(
                _mockUnitOfWork.Object,
                _mockEmailService.Object,
                _mockJwtProvider.Object,
                _mockTurnstileService.Object,
                _cache);
        }

        [Fact]
        public async Task LoginAsync_InvalidTurnstileToken_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new LoginRequest("user@email.com", "Password123!", "invalid-token");
            _mockTurnstileService
                .Setup(s => s.VerifyTokenAsync(request.TurnstileToken))
                .ReturnsAsync(false);

            // 2. ACT
            var result = await _sut.LoginAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Xác thực CAPTCHA thất bại");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_NonExistingAccount_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new LoginRequest("missing@email.com", "Password123!", "valid-token");
            _mockTurnstileService
                .Setup(s => s.VerifyTokenAsync(request.TurnstileToken))
                .ReturnsAsync(true);
            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>() ))
                .ReturnsAsync((User)null!);

            // 2. ACT
            var result = await _sut.LoginAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Tài khoản hoặc mật khẩu không chính xác");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsSuccessResult()
        {
            // 1. ARRANGE
            var password = "Password123!";
            var account = new User
            {
                Email = "user@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                UserStatus = UserStatus.Active,
                IsActive = true,
                UserId = "user-123",
                Role = RoleEnum.USER
            };

            var request = new LoginRequest(account.Email, password, "valid-token");
            _mockTurnstileService
                .Setup(s => s.VerifyTokenAsync(request.TurnstileToken))
                .ReturnsAsync(true);
            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(account);
            _mockJwtProvider
                .Setup(j => j.GenerateToken(account))
                .Returns("generated-jwt-token");

            // 2. ACT
            var result = await _sut.LoginAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Đăng nhập thành công.");
            result.Data.Should().NotBeNull();
            result.Data?.AccessToken.Should().Be("generated-jwt-token");
            result.Data?.Message.Should().Contain(account.UserId);
        }

        [Fact]
        public async Task RegisterAsync_EmailAlreadyExists_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new RegisterRequest("existing@email.com", "Password123!", "0123456789", "username", "name", "valid-token");
            _mockTurnstileService
                .Setup(s => s.VerifyTokenAsync(request.TurnstileToken))
                .ReturnsAsync(true);
            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new User { Email = request.Email, IsActive = true });

            // 2. ACT
            var result = await _sut.RegisterAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Email này đã được sử dụng hệ thống");
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_ReturnsSuccessResult()
        {
            // 1. ARRANGE
            var request = new RegisterRequest("newuser@email.com", "Password123!", "0123456789", "username", "name", "valid-token");
            _mockTurnstileService
                .Setup(s => s.VerifyTokenAsync(request.TurnstileToken))
                .ReturnsAsync(true);
            _mockUserRepository!
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))!
                .ReturnsAsync((User?)null);
            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockEmailService
                .Setup(e => e.ResendOtpEmailAsync(request.Email, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // 2. ACT
            var result = await _sut.RegisterAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("Đăng ký thành công");
            _mockUserRepository.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == request.Email && u.IsActive == false)), Times.Once);
            var cacheValue = await _cache.GetStringAsync($"OTP:Register:{request.Email.Trim().ToLowerInvariant()}");
            cacheValue.Should().NotBeNullOrEmpty();
            _mockEmailService.Verify(e => e.ResendOtpEmailAsync(request.Email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task VerifyOtpAsync_InvalidOtp_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new VerifyOtpRequest("user@email.com", "000000");
            await _cache.SetStringAsync($"OTP:Register:{request.Email.Trim().ToLowerInvariant()}", "123456", new DistributedCacheEntryOptions());

            // 2. ACT
            var result = await _sut.VerifyOtpAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Mã OTP không chính xác hoặc đã hết hạn");
        }

        [Fact]
        public async Task VerifyOtpAsync_ValidOtpAndExistingAccount_ReturnsSuccessResult()
        {
            // 1. ARRANGE
            var request = new VerifyOtpRequest("verified@email.com", "123456");
            var account = new User { Email = request.Email, IsActive = false, UserId = "user-456", Role = RoleEnum.USER };
            await _cache.SetStringAsync($"OTP:Register:{request.Email.Trim().ToLowerInvariant()}", request.OtpCode, new DistributedCacheEntryOptions());

            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>() ))
                .ReturnsAsync(account);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockJwtProvider
                .Setup(j => j.GenerateToken(account))
                .Returns("otp-token");

            // 2. ACT
            var result = await _sut.VerifyOtpAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("Xác thực OTP thành công");
            result.Data.Should().NotBeNull();
            result.Data?.AccessToken.Should().Be("otp-token");
            account.IsActive.Should().BeTrue();
            var removedValue = await _cache.GetStringAsync($"OTP:Register:{request.Email.Trim().ToLowerInvariant()}");
            removedValue.Should().BeNull();
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidOtp_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new ResetPasswordRequest("user@email.com", "000000", "NewPassword123!");
            await _cache.SetStringAsync($"OTP:Password:{request.Email.Trim().ToLowerInvariant()}", string.Empty, new DistributedCacheEntryOptions());
            await _cache.RemoveAsync($"OTP:Password:{request.Email.Trim().ToLowerInvariant()}");

            // 2. ACT
            var result = await _sut.ResetPasswordAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Mã OTP không chính xác hoặc đã hết hạn");
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidOtpAndExistingAccount_ReturnsSuccessResult()
        {
            // 1. ARRANGE
            var request = new ResetPasswordRequest("user@email.com", "123456", "NewPassword123!");
            var account = new User { Email = request.Email, Password = BCrypt.Net.BCrypt.HashPassword("OldPassword!") };
            await _cache.SetStringAsync($"OTP:Password:{request.Email.Trim().ToLowerInvariant()}", request.OtpCode, new DistributedCacheEntryOptions());
            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(account);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.ResetPasswordAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("Đặt lại mật khẩu thành công");
            BCrypt.Net.BCrypt.Verify(request.NewPassword, account.Password).Should().BeTrue();
            var removedOtp = await _cache.GetStringAsync($"OTP:Password:{request.Email.Trim().ToLowerInvariant()}");
            removedOtp.Should().BeNull();
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCurrentPassword_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new ChangePasswordRequest("user@email.com", "WrongPassword!", "NewPassword123!");
            var account = new User { Email = request.Email, Password = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!") };

            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(account);

            // 2. ACT
            var result = await _sut.ChangePasswordAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Mật khẩu hiện tại không chính xác");
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidCurrentPassword_ReturnsSuccessResult()
        {
            // 1. ARRANGE
            var request = new ChangePasswordRequest("user@email.com", "CurrentPassword1!", "NewPassword123!");
            var account = new User { Email = request.Email, Password = BCrypt.Net.BCrypt.HashPassword(request.CurrentPassword) };

            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(account);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.ChangePasswordAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("Đổi mật khẩu thành công");
            BCrypt.Net.BCrypt.Verify(request.NewPassword, account.Password).Should().BeTrue();
        }
    }

    internal class TestDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _storage = new();

        public byte[] Get(string key)
        {
            return _storage.TryGetValue(key, out var value) ? value : null!;
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _storage.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _storage[key] = value;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}

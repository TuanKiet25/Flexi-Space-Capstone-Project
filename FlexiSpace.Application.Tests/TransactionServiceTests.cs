using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Requests;
using Microsoft.AspNetCore.Http;
using Moq;
using PayOS;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class TransactionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly TransactionService _sut;

        public TransactionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _sut = new TransactionService(_mockUnitOfWork.Object, (PayOSClient)null!, _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task CreateTransactionAsync_MissingUrls_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new TransactionRequest { Amount = 100, ReturnUrl = null, CancelUrl = "https://cancel.test" };

            // 2. ACT
            var result = await _sut.CreateTransactionAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("ReturnUrl and CancelUrl are required.");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task CreateTransactionAsync_NonPositiveAmount_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new TransactionRequest { Amount = 0, ReturnUrl = "https://return.test", CancelUrl = "https://cancel.test" };

            // 2. ACT
            var result = await _sut.CreateTransactionAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Amount must be greater than zero.");
        }

        [Fact]
        public async Task CreateTransactionAsync_MissingTokenUser_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new TransactionRequest { Amount = 100, ReturnUrl = "https://return.test", CancelUrl = "https://cancel.test" };
            _mockHttpContextAccessor.SetupGet(a => a.HttpContext).Returns(new DefaultHttpContext());

            // 2. ACT
            var result = await _sut.CreateTransactionAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Invalid user ID from token");
        }

        [Fact]
        public async Task CreateTransactionAsync_ValidUserButPaymentClientMissing_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new TransactionRequest { Amount = 100, ReturnUrl = "https://return.test", CancelUrl = "https://cancel.test" };
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-1")
                }))
            };
            _mockHttpContextAccessor.SetupGet(a => a.HttpContext).Returns(context);

            // 2. ACT
            var result = await _sut.CreateTransactionAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().NotBeNullOrWhiteSpace();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }
}

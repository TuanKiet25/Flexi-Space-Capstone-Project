using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class TransactionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IWalletRepository> _mockWalletRepository;
        private readonly Mock<ITransactionRepository> _mockTransactionRepository;
        private readonly Mock<ITransactionHistoryRepository> _mockTransactionHistoryRepository;
        private readonly Mock<IPayOSGateway> _mockPayOSGateway;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly TransactionService _sut;

        public TransactionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockWalletRepository = new Mock<IWalletRepository>();
            _mockTransactionRepository = new Mock<ITransactionRepository>();
            _mockTransactionHistoryRepository = new Mock<ITransactionHistoryRepository>();
            _mockPayOSGateway = new Mock<IPayOSGateway>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _mockUnitOfWork.SetupGet(u => u.walletRepository).Returns(_mockWalletRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.transactionRepository).Returns(_mockTransactionRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.transactionHistoryRepository).Returns(_mockTransactionHistoryRepository.Object);

            _sut = new TransactionService(_mockUnitOfWork.Object, _mockPayOSGateway.Object, _mockHttpContextAccessor.Object);
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
        public async Task CreateTransactionAsync_ValidUserButPaymentGatewayFails_ReturnsFailedResult()
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
            _mockWalletRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Wallet, bool>>>()))
                .ReturnsAsync(new Wallet { Id = 1, UserId = "user-1" });
            _mockPayOSGateway
                .Setup(g => g.CreatePaymentLinkAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("PayOS failed"));

            // 2. ACT
            var result = await _sut.CreateTransactionAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("PayOS failed");
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateTransactionAsync_ValidRequest_CreatesPendingTransactionAndReturnsCheckoutUrl()
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
            _mockWalletRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Wallet, bool>>>()))
                .ReturnsAsync(new Wallet { Id = 7, UserId = "user-1" });
            _mockPayOSGateway
                .Setup(g => g.CreatePaymentLinkAsync(
                    It.IsAny<int>(),
                    100,
                    "Top up wallet: 100",
                    "https://return.test",
                    "https://cancel.test"))
                .ReturnsAsync("https://checkout.test");
            _mockTransactionRepository
                .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.CreateTransactionAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("https://checkout.test");
            _mockTransactionRepository.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
                t.UserId == "user-1" &&
                t.WalletId == 7 &&
                t.Amount == 100 &&
                t.Status == TransactionEnum.Pending &&
                !t.IsDeleted)), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyWebhookSuccess_TestOrderCode_ReturnsTrue()
        {
            // 1. ARRANGE
            _mockPayOSGateway
                .Setup(g => g.VerifyWebhookOrderCodeAsync(It.IsAny<PayOS.Models.Webhooks.Webhook>()))
                .ReturnsAsync(123);

            // 2. ACT
            var result = await _sut.VerifyWebhookSuccess(new PayOS.Models.Webhooks.Webhook());

            // 3. ASSERT
            result.Should().BeTrue();
            _mockTransactionRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<Transaction, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task VerifyWebhookSuccess_ExistingWallet_CompletesTransactionAndAddsHistory()
        {
            // 1. ARRANGE
            var transaction = new Transaction
            {
                UserId = "user-1",
                Amount = 250,
                TransactionCode = 456,
                Status = TransactionEnum.Pending
            };
            var wallet = new Wallet { Id = 9, UserId = "user-1", Balance = 1000 };

            _mockPayOSGateway
                .Setup(g => g.VerifyWebhookOrderCodeAsync(It.IsAny<PayOS.Models.Webhooks.Webhook>()))
                .ReturnsAsync(456);
            _mockTransactionRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync(transaction);
            _mockWalletRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Wallet, bool>>>()))
                .ReturnsAsync(wallet);
            _mockTransactionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);
            _mockWalletRepository
                .Setup(r => r.UpdateAsync(wallet))
                .Returns(Task.CompletedTask);
            _mockTransactionHistoryRepository
                .Setup(r => r.AddAsync(It.IsAny<TransactionHistory>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.VerifyWebhookSuccess(new PayOS.Models.Webhooks.Webhook());

            // 3. ASSERT
            result.Should().BeTrue();
            transaction.Status.Should().Be(TransactionEnum.Completed);
            transaction.WalletId.Should().Be(9);
            wallet.Balance.Should().Be(1250);
            _mockTransactionHistoryRepository.Verify(r => r.AddAsync(It.Is<TransactionHistory>(h =>
                h.WalletId == 9 &&
                h.TransactionAmount == 250 &&
                h.Status == TransactionEnum.Completed &&
                h.Description == "Top up wallet: 250")), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyWebhookSuccess_MissingWallet_CreatesWalletAndAddsHistory()
        {
            // 1. ARRANGE
            var transaction = new Transaction
            {
                UserId = "user-1",
                Amount = 250,
                TransactionCode = 456,
                Status = TransactionEnum.Pending
            };

            _mockPayOSGateway
                .Setup(g => g.VerifyWebhookOrderCodeAsync(It.IsAny<PayOS.Models.Webhooks.Webhook>()))
                .ReturnsAsync(456);
            _mockTransactionRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync(transaction);
            _mockWalletRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Wallet, bool>>>()))
                .ReturnsAsync((Wallet)null!);
            _mockWalletRepository
                .Setup(r => r.AddAsync(It.IsAny<Wallet>()))
                .Returns(Task.CompletedTask);
            _mockTransactionRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);
            _mockTransactionHistoryRepository
                .Setup(r => r.AddAsync(It.IsAny<TransactionHistory>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.VerifyWebhookSuccess(new PayOS.Models.Webhooks.Webhook());

            // 3. ASSERT
            result.Should().BeTrue();
            transaction.Status.Should().Be(TransactionEnum.Completed);
            transaction.Wallet.Should().NotBeNull();
            transaction.Wallet!.Balance.Should().Be(250);
            _mockWalletRepository.Verify(r => r.AddAsync(It.Is<Wallet>(w =>
                w.UserId == "user-1" &&
                w.Balance == 250 &&
                w.IsActive &&
                !w.IsDeleted)), Times.Once);
            _mockWalletRepository.Verify(r => r.UpdateAsync(It.IsAny<Wallet>()), Times.Never);
            _mockTransactionHistoryRepository.Verify(r => r.AddAsync(It.IsAny<TransactionHistory>()), Times.Once);
        }

        [Fact]
        public async Task VerifyWebhookSuccess_MissingTransaction_ThrowsWrappedException()
        {
            // 1. ARRANGE
            _mockPayOSGateway
                .Setup(g => g.VerifyWebhookOrderCodeAsync(It.IsAny<PayOS.Models.Webhooks.Webhook>()))
                .ReturnsAsync(456);
            _mockTransactionRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync((Transaction)null!);

            // 2. ACT
            var act = async () => await _sut.VerifyWebhookSuccess(new PayOS.Models.Webhooks.Webhook());

            // 3. ASSERT
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Error handling webhook:*");
        }
    }
}

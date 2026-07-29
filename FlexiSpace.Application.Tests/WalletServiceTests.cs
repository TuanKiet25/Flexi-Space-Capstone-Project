using AutoMapper;
using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class WalletServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IWalletRepository> _mockWalletRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly WalletService _sut;

        public WalletServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockWalletRepository = new Mock<IWalletRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.SetupGet(u => u.walletRepository).Returns(_mockWalletRepository.Object);

            _sut = new WalletService(_mockUnitOfWork.Object, _mockMapper.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task GetAllWallet_RepositoryReturnsWallets_ReturnsMappedSuccessResult()
        {
            // 1. ARRANGE
            var wallets = new List<Wallet> { new() { Id = 1, Balance = 100 } };
            var response = new List<WalletRespnse> { new() { Id = 1, Balance = 100 } };
            _mockWalletRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Wallet, bool>>>(),
                    It.IsAny<Func<IQueryable<Wallet>, IIncludableQueryable<Wallet, object>>>()))
                .ReturnsAsync(wallets);
            _mockMapper
                .Setup(m => m.Map<IEnumerable<WalletRespnse>>(wallets))
                .Returns(response);

            // 2. ACT
            var result = await _sut.GetAllWallet();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task GetOwnWallet_MissingCurrentUser_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns(string.Empty);

            // 2. ACT
            var result = await _sut.GetOwnWallet();

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("User is not authenticated.");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetWalletByUserId_WalletMissing_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            _mockWalletRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Wallet, bool>>>(),
                    It.IsAny<Func<IQueryable<Wallet>, IIncludableQueryable<Wallet, object>>>()))
                .ReturnsAsync((Wallet)null!);

            // 2. ACT
            var result = await _sut.GetWalletByUserId("user-1");

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Be("Wallet not found.");
        }

        [Fact]
        public async Task SpendWalletBalance_InsufficientBalance_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockWalletRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Wallet, bool>>>(),
                    It.IsAny<Func<IQueryable<Wallet>, IIncludableQueryable<Wallet, object>>>()))
                .ReturnsAsync(new Wallet { Id = 1, UserId = "user-1", Balance = 50 });

            // 2. ACT
            var result = await _sut.SpendWalletBalance(100);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Balance not enough");
            _mockWalletRepository.Verify(r => r.UpdateAsync(It.IsAny<Wallet>()), Times.Never);
        }

        [Fact]
        public async Task SpendWalletBalance_EnoughBalance_SubtractsAbsoluteAmountAndSaves()
        {
            // 1. ARRANGE
            var wallet = new Wallet { Id = 1, UserId = "user-1", Balance = 100 };
            var response = new WalletRespnse { Id = 1, Balance = 70 };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockWalletRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Wallet, bool>>>(),
                    It.IsAny<Func<IQueryable<Wallet>, IIncludableQueryable<Wallet, object>>>()))
                .ReturnsAsync(wallet);
            _mockWalletRepository
                .Setup(r => r.UpdateAsync(wallet))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<WalletRespnse>(wallet))
                .Returns(response);

            // 2. ACT
            var result = await _sut.SpendWalletBalance(-30);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            wallet.Balance.Should().Be(70);
            wallet.UpdatedBy.Should().Be("user-1");
            _mockWalletRepository.Verify(r => r.UpdateAsync(wallet), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateWalletBalance_NegativeBalanceFloor_SetsBalanceToZero()
        {
            // 1. ARRANGE
            var wallet = new Wallet { Id = 1, UserId = "user-1", Balance = 20 };
            var response = new WalletRespnse { Id = 1, Balance = 0 };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("admin-1");
            _mockWalletRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Wallet, bool>>>(),
                    It.IsAny<Func<IQueryable<Wallet>, IIncludableQueryable<Wallet, object>>>()))
                .ReturnsAsync(wallet);
            _mockWalletRepository
                .Setup(r => r.UpdateAsync(wallet))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<WalletRespnse>(wallet))
                .Returns(response);

            // 2. ACT
            var result = await _sut.UpdateWalletBalance("user-1", -100);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            wallet.Balance.Should().Be(0);
            wallet.UpdatedBy.Should().Be("admin-1");
        }

        [Fact]
        public async Task UpdateWalletBalance_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockWalletRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Wallet, bool>>>(),
                    It.IsAny<Func<IQueryable<Wallet>, IIncludableQueryable<Wallet, object>>>()))
                .ThrowsAsync(new InvalidOperationException("Db failure"));

            // 2. ACT
            var result = await _sut.UpdateWalletBalance("user-1", 100);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Error updating wallet balance: Db failure");
        }
    }
}

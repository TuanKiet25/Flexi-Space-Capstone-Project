using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace FlexiSpace.Application.Tests
{
    public class DashboardServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IWalletRepository> _mockWalletRepository;
        private readonly Mock<ITransactionHistoryRepository> _mockTransactionHistoryRepository;
        private readonly DashboardService _sut;

        public DashboardServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockWalletRepository = new Mock<IWalletRepository>();
            _mockTransactionHistoryRepository = new Mock<ITransactionHistoryRepository>();

            _mockUnitOfWork.SetupGet(u => u.walletRepository).Returns(_mockWalletRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.transactionHistoryRepository).Returns(_mockTransactionHistoryRepository.Object);

            _sut = new DashboardService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_Success_ReturnsAggregatedData()
        {
            // 1. ARRANGE
            var wallets = new List<Wallet>
            {
                new() { Id = 1, Balance = 150000, IsDeleted = false },
                new() { Id = 2, Balance = 250000, IsDeleted = false }
            };

            var histories = new List<TransactionHistory>
            {
                new() { Id = 1, TransactionAmount = -50000, Description = "Thanh toán bài đăng", Status = TransactionEnum.Completed, IsDeleted = false },
                new() { Id = 2, TransactionAmount = -2000, Description = "Thanh toán sử dụng công cụ AI", Status = TransactionEnum.Completed, IsDeleted = false },
                new() { Id = 3, TransactionAmount = -2000, Description = "Thanh toán sử dụng công cụ AI", Status = TransactionEnum.Completed, IsDeleted = false },
                new() { Id = 4, TransactionAmount = -10000, Description = "Thanh toán khác", Status = TransactionEnum.Completed, IsDeleted = false } // Should be ignored
            };

            _mockWalletRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Wallet, bool>>>()))
                .ReturnsAsync(wallets);

            _mockTransactionHistoryRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<TransactionHistory, bool>>>()))
                .ReturnsAsync(histories);

            // 2. ACT
            var result = await _sut.GetDashboardStatsAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalWalletBalance.Should().Be(400000); // 150000 + 250000
            result.Data.TotalListingSpent.Should().Be(50000); // |-50000|
            result.Data.TotalListingCount.Should().Be(1);
            result.Data.TotalAiImageSpent.Should().Be(4000); // |-2000| * 2
            result.Data.TotalAiImageCount.Should().Be(2);
            result.Data.TotalSpent.Should().Be(54000); // 50000 + 4000
            result.Message.Should().Be("Lấy dữ liệu dashboard thành công.");
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ExceptionThrown_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockWalletRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Wallet, bool>>>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // 2. ACT
            var result = await _sut.GetDashboardStatsAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.Message.Should().Contain("Lỗi khi tải dữ liệu dashboard: Database connection failed");
        }
    }
}

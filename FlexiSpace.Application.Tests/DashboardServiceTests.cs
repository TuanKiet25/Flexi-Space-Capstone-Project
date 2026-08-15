using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
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
        private readonly Mock<IListingRepository> _mockListingRepository;
        private readonly Mock<IListingViewDailyStatRepository> _mockListingViewDailyStatRepository;
        private readonly Mock<IPrimaryBookingRequestRepository> _mockPrimaryBookingRequestRepository;
        private readonly Mock<IContractRepository> _mockContractRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly DashboardService _sut;

        public DashboardServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockWalletRepository = new Mock<IWalletRepository>();
            _mockTransactionHistoryRepository = new Mock<ITransactionHistoryRepository>();
            _mockListingRepository = new Mock<IListingRepository>();
            _mockListingViewDailyStatRepository = new Mock<IListingViewDailyStatRepository>();
            _mockPrimaryBookingRequestRepository = new Mock<IPrimaryBookingRequestRepository>();
            _mockContractRepository = new Mock<IContractRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.SetupGet(u => u.walletRepository).Returns(_mockWalletRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.transactionHistoryRepository).Returns(_mockTransactionHistoryRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.listingRepository).Returns(_mockListingRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.listingViewDailyStatRepository).Returns(_mockListingViewDailyStatRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.primaryBookingRequestRepository).Returns(_mockPrimaryBookingRequestRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.contractRepository).Returns(_mockContractRepository.Object);

            _sut = new DashboardService(_mockUnitOfWork.Object, _mockCurrentUserService.Object);
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

        [Fact]
        public async Task GetListingOverviewAsync_MissingCurrentUser_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns(" ");

            // 2. ACT
            var result = await _sut.GetListingOverviewAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Register first!");
            _mockListingRepository.Verify(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task GetListingOverviewAsync_ValidOwner_ReturnsFourDashboardChartSeries()
        {
            // 1. ARRANGE
            var today = DateTime.Now.Date;
            var listings = new List<Listing>
            {
                new() { Id = 1, CreatorId = "owner-1", Status = ListingStatusEnum.Available, IsActive = true, IsDeleted = false, CreatedAt = today.AddDays(-1), durationInDays = 4 },
                new() { Id = 2, CreatorId = "owner-1", Status = ListingStatusEnum.Expired, IsActive = false, IsDeleted = false, CreatedAt = today.AddDays(-3), durationInDays = 1 },
                new() { Id = 3, CreatorId = "owner-1", Status = ListingStatusEnum.Available, IsActive = true, IsDeleted = false, CreatedAt = today.AddDays(-8), durationInDays = 2 }
            };
            var viewStats = new List<ListingViewDailyStat>
            {
                new() { ListingId = 1, Date = DateOnly.FromDateTime(today), ViewCount = 4 },
                new() { ListingId = 3, Date = DateOnly.FromDateTime(today.AddDays(-1)), ViewCount = 6 }
            };
            var bookingRequests = new List<PrimaryBookingRequest>
            {
                new() { ListingId = 1, LessorId = "owner-1", CreatedAt = today, IsDeleted = false }
            };
            var contracts = new List<Contract>
            {
                new() { LessorId = "owner-1", Status = ContractStatusEnum.Active, CreatedAt = today.AddMonths(-1), IsDeleted = false },
                new() { LessorId = "owner-1", Status = ContractStatusEnum.Active, CreatedAt = today, IsDeleted = false }
            };

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("owner-1");
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(listings);
            _mockListingViewDailyStatRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ListingViewDailyStat, bool>>>()))
                .ReturnsAsync(viewStats);
            _mockPrimaryBookingRequestRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()))
                .ReturnsAsync(bookingRequests);
            _mockContractRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(contracts);

            // 2. ACT
            var result = await _sut.GetListingOverviewAsync(rangeDays: 7, futureDays: 3);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalListings.Should().Be(3);
            result.Data.ActiveListings.Should().Be(2);
            result.Data.SignedContracts.Should().Be(2);
            result.Data.ExpiredListings.Should().Be(1);
            result.Data.ActiveListingViewsLastPeriod.Should().Be(10);
            result.Data.ActiveListingBookingRequestsLastPeriod.Should().Be(1);
            result.Data.NewListingsTrend.Should().HaveCount(7);
            result.Data.ActiveInteractionTrend.Should().HaveCount(7);
            result.Data.SignedContractsTrend.Should().HaveCount(6);
            result.Data.ExpiredTrend.Should().HaveCount(10);
            result.Data.ActiveInteractionTrend.Should().Contain(x => x.Views == 4 && x.BookingRequests == 1);
            result.Data.ExpiredTrend.Should().Contain(x => x.Value == 1 && !x.IsFuture);
            result.Data.ExpiredTrend.Should().Contain(x => x.Value == 1 && x.IsFuture);
        }
    }
}

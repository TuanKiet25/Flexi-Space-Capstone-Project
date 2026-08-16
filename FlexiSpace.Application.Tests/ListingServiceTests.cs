using AutoMapper;
using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class ListingServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IListingRepository> _mockListingRepository;
        private readonly Mock<ISpaceRepository> _mockSpaceRepository;
        private readonly Mock<IAmentityRepository> _mockAmenityRepository;
        private readonly Mock<IListingReportRepository> _mockListingReportRepository;
        private readonly Mock<IListingViewDailyStatRepository> _mockListingViewDailyStatRepository;
        private readonly Mock<ISpaceUsageRightRepository> _mockSpaceUsageRightRepository;
        private readonly Mock<IBannerRepository> _mockBannerRepository;
        private readonly Mock<IWalletService> _mockWalletService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly ListingService _sut;

        public ListingServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockListingRepository = new Mock<IListingRepository>();
            _mockSpaceRepository = new Mock<ISpaceRepository>();
            _mockAmenityRepository = new Mock<IAmentityRepository>();
            _mockListingReportRepository = new Mock<IListingReportRepository>();
            _mockListingViewDailyStatRepository = new Mock<IListingViewDailyStatRepository>();
            _mockSpaceUsageRightRepository = new Mock<ISpaceUsageRightRepository>();
            _mockBannerRepository = new Mock<IBannerRepository>();
            _mockWalletService = new Mock<IWalletService>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCache = new Mock<IDistributedCache>();

            _mockUnitOfWork.SetupGet(u => u.listingRepository).Returns(_mockListingRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.spaceRepository).Returns(_mockSpaceRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.amenityRepository).Returns(_mockAmenityRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.listingReportRepository).Returns(_mockListingReportRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.listingViewDailyStatRepository).Returns(_mockListingViewDailyStatRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.spaceUsageRightRepository).Returns(_mockSpaceUsageRightRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.bannerRepository).Returns(_mockBannerRepository.Object);

            _mockCache
                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);
            _mockCache
                .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCache
                .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _sut = new ListingService(_mockUnitOfWork.Object, _mockWalletService.Object, _mockMapper.Object, _mockCurrentUserService.Object, _mockCache.Object);
        }

        [Fact]
        public async Task CreateListingAsync_WalletSpendFails_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = CreateListingRequest();
            _mockWalletService
                .Setup(s => s.SpendWalletBalance(50, "Thanh toán bài đăng"))
                .ReturnsAsync(new ServiceResult<WalletRespnse> { IsSuccess = false, Message = "Balance not enough" });

            // 2. ACT
            var result = await _sut.CreateListingAsync(request, 50, 30);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Balance not enough");
            _mockSpaceRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>(), It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()), Times.Never);
            _mockListingRepository.Verify(r => r.AddAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task CreateListingAsync_SpaceNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = CreateListingRequest();
            SetupWalletSpendSuccess();
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync((Space)null!);

            // 2. ACT
            var result = await _sut.CreateListingAsync(request, 50, 30);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy");
            _mockListingRepository.Verify(r => r.AddAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task CreateListingAsync_ValidRequest_CreatesAcceptedEntireSpaceListing()
        {
            // 1. ARRANGE
            var request = CreateListingRequest();
            var listing = new Listing { Id = 5, SpaceId = request.SpaceId, Price = request.Price };
            var response = new ListingResponse
            {
                Id = 5,
                Price = request.Price,
                CreatorId = "lessor-1",
                LessorName = "Lessor",
                SpaceAddress = "Address",
                SpaceCity= "City"
            };
            SetupWalletSpendSuccess();
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync(new Space { Id = request.SpaceId, OwnerId = "lessor-1" });
            _mockMapper
                .Setup(m => m.Map<Listing>(request))
                .Returns(listing);
            _mockListingRepository
                .Setup(r => r.AddAsync(listing))
                .Returns(Task.CompletedTask);
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing>());
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(listing);
            _mockMapper
                .Setup(m => m.Map<ListingResponse>(listing))
                .Returns(response);

            // 2. ACT
            var result = await _sut.CreateListingAsync(request, 50, 30);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            listing.CreatorId.Should().Be("lessor-1");
            listing.IsActive.Should().BeTrue();
            listing.Status.Should().Be(ListingStatusEnum.Available);
            listing.ListingType.Should().Be(ListingType.EntireSpace);
            _mockListingRepository.Verify(r => r.AddAsync(listing), Times.Once);
        }

        [Fact]
        public async Task CreateListingAsync_PriceUnitExceedsListingPeriod_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = CreateListingRequest();
            request.AllowedEndTime = request.AllowedStartTime!.Value.AddDays(30);
            request.PriceUnit = PriceUnit.PerYear;

            SetupWalletSpendSuccess();
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync(new Space { Id = request.SpaceId, OwnerId = "lessor-1" });

            // 2. ACT
            var result = await _sut.CreateListingAsync(request, 50, 30);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain(nameof(PriceUnit.PerMonth));
            _mockListingRepository.Verify(r => r.AddAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task GetListingByIdAsync_ListingNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync((Listing)null!);

            // 2. ACT
            var result = await _sut.GetListingByIdAsync(99);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy listing");
        }

        [Fact]
        public async Task GetListingByIdAsync_ListingExists_ReturnsMappedListing()
        {
            // 1. ARRANGE
            var listing = new Listing { Id = 5, SpaceId = 10 };
            var response = CreateListingResponse(5);

            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(listing);
            _mockMapper
                .Setup(m => m.Map<ListingResponse>(listing))
                .Returns(response);

            // 2. ACT
            var result = await _sut.GetListingByIdAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            _mockCache.Verify(c => c.SetAsync(
                "Cache:Listing:Id_5",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IncreaseViewCountAsync_ListingExists_IncrementsViewCountAndReturnsResponse()
        {
            // 1. ARRANGE
            var listing = new Listing { Id = 5, viewCount = 7, IsDeleted = false };
            var response = CreateListingResponse(5);
            response.ViewCount = 8;

            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(listing);
            _mockListingRepository
                .Setup(r => r.UpdateAsync(listing))
                .Returns(Task.CompletedTask);
            _mockListingViewDailyStatRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ListingViewDailyStat, bool>>>()))
                .ReturnsAsync((ListingViewDailyStat)null!);
            _mockListingViewDailyStatRepository
                .Setup(r => r.AddAsync(It.IsAny<ListingViewDailyStat>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<ListingResponse>(listing))
                .Returns(response);

            // 2. ACT
            var result = await _sut.IncreaseViewCountAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            result.Data!.ViewCount.Should().Be(8);
            result.Message.Should().Be("Tăng lượt xem listing thành công.");
            listing.viewCount.Should().Be(8);
            listing.UpdatedAt.Should().NotBe(default);
            _mockListingViewDailyStatRepository.Verify(r => r.AddAsync(It.Is<ListingViewDailyStat>(x =>
                x.ListingId == 5 &&
                x.Date == DateOnly.FromDateTime(DateTime.Now) &&
                x.ViewCount == 1)), Times.Once);
            _mockListingViewDailyStatRepository.Verify(r => r.UpdateAsync(It.IsAny<ListingViewDailyStat>()), Times.Never);
            _mockListingRepository.Verify(r => r.UpdateAsync(listing), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task IncreaseViewCountAsync_DailyStatExists_IncrementsDailyStat()
        {
            // 1. ARRANGE
            var listing = new Listing { Id = 5, viewCount = 7, IsDeleted = false };
            var dailyStat = new ListingViewDailyStat
            {
                ListingId = 5,
                Date = DateOnly.FromDateTime(DateTime.Now),
                ViewCount = 3
            };
            var response = CreateListingResponse(5);
            response.ViewCount = 8;

            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(listing);
            _mockListingViewDailyStatRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ListingViewDailyStat, bool>>>()))
                .ReturnsAsync(dailyStat);
            _mockListingViewDailyStatRepository
                .Setup(r => r.UpdateAsync(dailyStat))
                .Returns(Task.CompletedTask);
            _mockListingRepository
                .Setup(r => r.UpdateAsync(listing))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<ListingResponse>(listing))
                .Returns(response);

            // 2. ACT
            var result = await _sut.IncreaseViewCountAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            listing.viewCount.Should().Be(8);
            dailyStat.ViewCount.Should().Be(4);
            dailyStat.UpdatedAt.Should().NotBe(default);
            _mockListingViewDailyStatRepository.Verify(r => r.UpdateAsync(dailyStat), Times.Once);
            _mockListingViewDailyStatRepository.Verify(r => r.AddAsync(It.IsAny<ListingViewDailyStat>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task IncreaseViewCountAsync_ListingNotFound_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync((Listing)null!);

            // 2. ACT
            var result = await _sut.IncreaseViewCountAsync(99);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Be("Không tìm thấy listing với Id đã cho.");
            _mockListingRepository.Verify(r => r.UpdateAsync(It.IsAny<Listing>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateListingAsync_ListingNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync((Listing)null!);

            // 2. ACT
            var result = await _sut.UpdateListingAsync(99, CreateListingRequest());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy listing");
            _mockListingRepository.Verify(r => r.UpdateAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task UpdateListingAsync_ValidRequest_UpdatesListing()
        {
            // 1. ARRANGE
            var request = CreateListingRequest();
            var listing = new Listing { Id = 5, SpaceId = request.SpaceId, CreatorId = "owner-1" };
            var response = CreateListingResponse(5, "owner-1");

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("owner-1");
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(listing);
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync(new Space { Id = request.SpaceId, OwnerId = "owner-1" });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing>());
            _mockMapper
                .Setup(m => m.Map(request, listing))
                .Returns(listing);
            _mockMapper
                .Setup(m => m.Map<ListingResponse>(listing))
                .Returns(response);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.UpdateListingAsync(5, request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            listing.UpdatedAt.Should().NotBe(default);
            _mockListingRepository.Verify(r => r.UpdateAsync(listing), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task HardDeleteListingAsync_ListingExists_RemovesListing()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new Listing { Id = 5 });
            _mockListingRepository
                .Setup(r => r.RemoveByIdAsync(5L))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.HardDeleteListingAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("listing");
            _mockListingRepository.Verify(r => r.RemoveByIdAsync(5L), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task HardDeleteListingAsync_ListingNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync((Listing)null!);

            // 2. ACT
            var result = await _sut.HardDeleteListingAsync(99);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockListingRepository.Verify(r => r.RemoveByIdAsync(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task AcceptOrCancelListingAsync_BanWithoutReason_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new Listing { Id = 5, Status = ListingStatusEnum.Available });

            // 2. ACT
            var result = await _sut.AcceptOrCancelListingAsync(5, new ListingStatusRequest { Status = ListingStatusEnum.Ban });

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("lý do");
            _mockListingRepository.Verify(r => r.UpdateAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task AcceptOrCancelListingAsync_BanWithReason_BansListingAndMarksReports()
        {
            // 1. ARRANGE
            var listing = new Listing { Id = 5, Status = ListingStatusEnum.Available, IsActive = true };
            var reports = new List<ListingReport>
            {
                new() { Id = 1, ListingId = 5 },
                new() { Id = 2, ListingId = 5 }
            };
            var response = CreateListingResponse(5);

            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(listing);
            _mockListingReportRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ListingReport, bool>>>()))
                .ReturnsAsync(reports);
            _mockMapper
                .Setup(m => m.Map<ListingResponse>(listing))
                .Returns(response);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.AcceptOrCancelListingAsync(5, new ListingStatusRequest { Status = ListingStatusEnum.Ban, CancelReason = "Fraud" });

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            listing.Status.Should().Be(ListingStatusEnum.Ban);
            listing.IsActive.Should().BeFalse();
            reports.Should().OnlyContain(x => x.IsBanned);
            _mockListingReportRepository.Verify(r => r.UpdateAsync(It.IsAny<ListingReport>()), Times.Exactly(2));
            _mockListingRepository.Verify(r => r.UpdateAsync(listing), Times.Once);
        }

        [Fact]
        public async Task CreateListingReportAsync_MissingReporter_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new CreateListingReportRequest { ListingId = 5, Reasons = new List<ReportReasonEnum> { ReportReasonEnum.Other } };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns(string.Empty);

            // 2. ACT
            var result = await _sut.CreateListingReportAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("đăng nhập");
            _mockListingReportRepository.Verify(r => r.AddAsync(It.IsAny<ListingReport>()), Times.Never);
        }

        [Fact]
        public async Task CreateListingReportAsync_ValidRequest_CreatesReport()
        {
            // 1. ARRANGE
            var request = new CreateListingReportRequest
            {
                ListingId = 5,
                Reasons = new List<ReportReasonEnum> { ReportReasonEnum.FakeInformation, ReportReasonEnum.PriceMismatch },
                AdditionalDetails = "Wrong details"
            };
            var response = new ListingReportResponse { ListingId = 5, ReporterId = "user-1" };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new Listing { Id = 5 });
            _mockListingReportRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ListingReport, bool>>>()))
                .ReturnsAsync((ListingReport)null!);
            _mockListingReportRepository
                .Setup(r => r.AddAsync(It.IsAny<ListingReport>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<ListingReportResponse>(It.IsAny<ListingReport>()))
                .Returns(response);

            // 2. ACT
            var result = await _sut.CreateListingReportAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            result.Message.Should().Contain("thành công");
            _mockListingReportRepository.Verify(r => r.AddAsync(It.Is<ListingReport>(x =>
                x.ListingId == 5 &&
                x.ReporterId == "user-1" &&
                x.ReasonType.Contains(nameof(ReportReasonEnum.FakeInformation)) &&
                x.ReasonType.Contains(nameof(ReportReasonEnum.PriceMismatch)) &&
                x.AdditionalDetails == "Wrong details")), Times.Once);
        }

        [Fact]
        public async Task GetListingReportDetailAsync_ReportsExist_ReturnsReasonBreakdown()
        {
            // 1. ARRANGE
            _mockListingReportRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ListingReport, bool>>>()))
                .ReturnsAsync(new List<ListingReport>
                {
                    new() { ListingId = 5, ReasonType = "FakeInformation,Other" },
                    new() { ListingId = 5, ReasonType = "FakeInformation" }
                });

            // 2. ACT
            var result = await _sut.GetListingReportDetailAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalReportCount.Should().Be(2);
            result.Data.ReasonBreakdown.Should().Contain(x => x.Reason == nameof(ReportReasonEnum.FakeInformation) && x.Count == 2);
            result.Data.ReasonBreakdown.Should().Contain(x => x.Reason == nameof(ReportReasonEnum.Other) && x.Count == 1);
        }

        [Fact]
        public async Task GetListingReportsAsync_ListingExists_ReturnsReportsNewestFirst()
        {
            // 1. ARRANGE
            var reports = new List<ListingReport>
            {
                new() { Id = 1, ListingId = 5, CreatedAt = DateTime.Now.AddDays(-2) },
                new() { Id = 2, ListingId = 5, CreatedAt = DateTime.Now }
            };
            var responses = new List<ListingReportResponse>
            {
                new() { Id = 2, ListingId = 5 },
                new() { Id = 1, ListingId = 5 }
            };

            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new Listing { Id = 5 });
            _mockListingReportRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<ListingReport, bool>>>(),
                    It.IsAny<Func<IQueryable<ListingReport>, IIncludableQueryable<ListingReport, object>>?>()))
                .ReturnsAsync(reports);
            _mockMapper
                .Setup(m => m.Map<List<ListingReportResponse>>(It.IsAny<List<ListingReport>>()))
                .Returns(responses);

            // 2. ACT
            var result = await _sut.GetListingReportsAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(responses);
        }

        [Fact]
        public async Task GetListingReportsAsync_ListingNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync((Listing)null!);

            // 2. ACT
            var result = await _sut.GetListingReportsAsync(99);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockListingReportRepository.Verify(r => r.GetAllAsync(
                It.IsAny<Expression<Func<ListingReport, bool>>>(),
                It.IsAny<Func<IQueryable<ListingReport>, IIncludableQueryable<ListingReport, object>>?>()), Times.Never);
        }

        [Fact]
        public async Task GetReportedListingsAsync_ReportsExist_ReturnsGroupedSummaries()
        {
            // 1. ARRANGE
            var reports = new List<ListingReport>
            {
                new() { ListingId = 5, IsBanned = false, Listing = new Listing { Id = 5, Status = ListingStatusEnum.Available, Description = "A" } },
                new() { ListingId = 5, IsBanned = true, Listing = new Listing { Id = 5, Status = ListingStatusEnum.Available, Description = "A" } },
                new() { ListingId = 6, IsBanned = false, Listing = new Listing { Id = 6, Status = ListingStatusEnum.Ban, Description = "B" } }
            };

            _mockListingReportRepository
                .Setup(r => r.GetAllWithSortAsync(
                    It.IsAny<Expression<Func<ListingReport, bool>>?>(),
                    It.IsAny<Func<IQueryable<ListingReport>, IIncludableQueryable<ListingReport, object>>?>(),
                    It.IsAny<Func<IQueryable<ListingReport>, IOrderedQueryable<ListingReport>>?>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(reports);

            // 2. ACT
            var result = await _sut.GetReportedListingsAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data![0].ListingId.Should().Be(5);
            result.Data[0].ReportCount.Should().Be(2);
            result.Data[0].IsBanned.Should().BeTrue();
            result.Data[1].ListingId.Should().Be(6);
        }

        [Fact]
        public async Task ClearListingReportsAsync_ReportsExist_RemovesReports()
        {
            // 1. ARRANGE
            var reports = new List<ListingReport>
            {
                new() { Id = 1, ListingId = 5 },
                new() { Id = 2, ListingId = 5 }
            };

            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new Listing { Id = 5 });
            _mockListingReportRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ListingReport, bool>>>()))
                .ReturnsAsync(reports);
            _mockListingReportRepository
                .Setup(r => r.RemoveRangeAsync(reports))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.ClearListingReportsAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(2);
            _mockListingReportRepository.Verify(r => r.RemoveRangeAsync(reports), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ClearListingReportsAsync_InvalidListingId_ReturnsFailedResult()
        {
            // 1. ARRANGE

            // 2. ACT
            var result = await _sut.ClearListingReportsAsync(0);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockListingRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task ClearListingReportsAsync_NoReports_ReturnsZero()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new Listing { Id = 5 });
            _mockListingReportRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ListingReport, bool>>>()))
                .ReturnsAsync(new List<ListingReport>());

            // 2. ACT
            var result = await _sut.ClearListingReportsAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
            _mockListingReportRepository.Verify(r => r.RemoveRangeAsync(It.IsAny<IEnumerable<ListingReport>>()), Times.Never);
        }

        [Fact]
        public async Task SoftDeleteListingAsync_ListingHasBanner_SoftDeletesBanner()
        {
            // 1. ARRANGE
            var listing = new Listing { Id = 5, IsDeleted = false, IsActive = true };
            var banner = new Banner { Id = 10, ListingId = 5, IsDeleted = false, IsActive = true };

            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(listing);
            _mockBannerRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Banner, bool>>>()))
                .ReturnsAsync(banner);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("admin-1");
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.SoftDeleteListingAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            listing.IsDeleted.Should().BeTrue();
            listing.IsActive.Should().BeFalse();
            banner.IsDeleted.Should().BeTrue();
            banner.IsActive.Should().BeFalse();
            banner.UpdatedBy.Should().Be("admin-1");
            _mockBannerRepository.Verify(r => r.UpdateAsync(banner), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteListingAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ThrowsAsync(new InvalidOperationException("Db failure"));

            // 2. ACT
            var result = await _sut.SoftDeleteListingAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Db failure");
        }

        [Fact]
        public async Task GetSoftDeletedListingsAsync_DeletedListingsExist_ReturnsMappedListings()
        {
            // 1. ARRANGE
            var listings = new List<Listing>
            {
                new() { Id = 1, IsDeleted = true, ListingType = ListingType.EntireSpace },
                new() { Id = 2, IsDeleted = true, ListingType = ListingType.SharedSpace }
            };
            var responses = new List<ShareListingResponse> { CreateShareListingResponse(1), CreateShareListingResponse(2) };

            SetupListingSortQuery(listings);
            _mockMapper
                .Setup(m => m.Map<List<ShareListingResponse>>(It.IsAny<List<Listing>>()))
                .Returns(responses);

            // 2. ACT
            var result = await _sut.GetSoftDeletedListingsAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(responses);
        }

        [Fact]
        public async Task RestoreListingAsync_DeletedListingExists_RestoresListing()
        {
            // 1. ARRANGE
            var listing = new Listing { Id = 5, IsDeleted = true, IsActive = false };
            var response = CreateListingResponse(5);

            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(listing);
            _mockMapper
                .Setup(m => m.Map<ListingResponse>(listing))
                .Returns(response);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.RestoreListingAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            listing.IsDeleted.Should().BeFalse();
            listing.IsActive.Should().BeTrue();
            _mockListingRepository.Verify(r => r.UpdateAsync(listing), Times.Once);
        }

        [Fact]
        public async Task RestoreListingAsync_DeletedListingNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync((Listing)null!);

            // 2. ACT
            var result = await _sut.RestoreListingAsync(99);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockListingRepository.Verify(r => r.UpdateAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task GetAllListingsAsync_NoStatus_ReturnsAvailableListingsOnly()
        {
            // 1. ARRANGE
            var listings = new List<Listing>
            {
                new() { Id = 1, Status = ListingStatusEnum.Available, IsDeleted = false, ListingType = ListingType.EntireSpace },
                new() { Id = 2, Status = ListingStatusEnum.Occupied, IsDeleted = false, ListingType = ListingType.EntireSpace },
                new() { Id = 3, Status = ListingStatusEnum.Expired, IsDeleted = false, ListingType = ListingType.SharedSpace }
            };
            var responses = new List<ShareListingResponse> { CreateShareListingResponse(1) };

            SetupListingSortQuery(listings);
            _mockMapper
                .Setup(m => m.Map<List<ShareListingResponse>>(It.IsAny<List<Listing>>()))
                .Returns(responses);

            // 2. ACT
            var result = await _sut.GetAllListingsAsync(null);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(responses);
        }

        [Fact]
        public async Task GetListingsByCurrentUserAsync_ExpiredStatus_ReturnsCurrentUsersExpiredListings()
        {
            // 1. ARRANGE
            var listings = new List<Listing>
            {
                new() { Id = 1, CreatorId = "user-1", Status = ListingStatusEnum.Expired, IsDeleted = false },
                new() { Id = 2, CreatorId = "user-2", Status = ListingStatusEnum.Expired, IsDeleted = false },
                new() { Id = 3, CreatorId = "user-1", Status = ListingStatusEnum.Available, IsDeleted = false }
            };
            var responses = new List<ShareListingResponse> { CreateShareListingResponse(1) };

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            SetupListingSortQuery(listings);
            _mockMapper
                .Setup(m => m.Map<List<ShareListingResponse>>(It.IsAny<List<Listing>>()))
                .Returns(responses);

            // 2. ACT
            var result = await _sut.GetListingsByCurrentUserAsync(ListingStatusEnum.Expired);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(responses);
        }

        [Fact]
        public async Task GetListingsByCurrentUserAsync_NullStatus_ReturnsAvailableOccupiedAndExpiredListings()
        {
            // 1. ARRANGE
            var listings = new List<Listing>
            {
                new() { Id = 1, CreatorId = "user-1", Status = ListingStatusEnum.Available, IsDeleted = false },
                new() { Id = 2, CreatorId = "user-1", Status = ListingStatusEnum.Occupied, IsDeleted = false },
                new() { Id = 3, CreatorId = "user-1", Status = ListingStatusEnum.Expired, IsDeleted = false },
                new() { Id = 4, CreatorId = "user-1", Status = ListingStatusEnum.Hidden, IsDeleted = false },
                new() { Id = 5, CreatorId = "user-2", Status = ListingStatusEnum.Available, IsDeleted = false }
            };
            List<Listing>? filteredListings = null;

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            SetupListingSortQuery(listings, x => filteredListings = x);
            _mockMapper
                .Setup(m => m.Map<List<ShareListingResponse>>(It.IsAny<List<Listing>>()))
                .Returns(new List<ShareListingResponse>
                {
                    CreateShareListingResponse(1),
                    CreateShareListingResponse(2),
                    CreateShareListingResponse(3)
                });

            // 2. ACT
            var result = await _sut.GetListingsByCurrentUserAsync(null);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            filteredListings.Should().NotBeNull();
            filteredListings!.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1L, 2L, 3L });
        }

        [Fact]
        public async Task GetListingsByUserIdAsync_NullStatus_ReturnsAvailableAndOccupiedListingsOnly()
        {
            // 1. ARRANGE
            var listings = new List<Listing>
            {
                new() { Id = 1, CreatorId = "user-1", Status = ListingStatusEnum.Available, IsDeleted = false },
                new() { Id = 2, CreatorId = "user-1", Status = ListingStatusEnum.Occupied, IsDeleted = false },
                new() { Id = 3, CreatorId = "user-1", Status = ListingStatusEnum.Expired, IsDeleted = false },
                new() { Id = 4, CreatorId = "user-1", Status = ListingStatusEnum.Hidden, IsDeleted = false },
                new() { Id = 5, CreatorId = "user-2", Status = ListingStatusEnum.Available, IsDeleted = false }
            };
            List<Listing>? filteredListings = null;

            SetupListingSortQuery(listings, x => filteredListings = x);
            _mockMapper
                .Setup(m => m.Map<List<ShareListingResponse>>(It.IsAny<List<Listing>>()))
                .Returns(new List<ShareListingResponse>
                {
                    CreateShareListingResponse(1),
                    CreateShareListingResponse(2)
                });

            // 2. ACT
            var result = await _sut.GetListingsByUserIdAsync("user-1", null);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            filteredListings.Should().NotBeNull();
            filteredListings!.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1L, 2L });
        }

        [Fact]
        public async Task GetListingsByUserIdAsync_MissingUserId_ReturnsFailedResult()
        {
            // 1. ACT
            var result = await _sut.GetListingsByUserIdAsync(" ", null);

            // 2. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("UserId is required.");
            _mockListingRepository.Verify(r => r.GetAllWithSortAsync(
                It.IsAny<Expression<Func<Listing, bool>>>(),
                It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>?>(),
                It.IsAny<Func<IQueryable<Listing>, IOrderedQueryable<Listing>>?>(),
                It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetShareListingTimePolicyAsync_MissingCurrentUser_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns(" ");

            // 2. ACT
            var result = await _sut.GetShareListingTimePolicyAsync(10);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Register first!");
            _mockSpaceRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task GetShareListingTimePolicyAsync_SpaceNotFound_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()))
                .ReturnsAsync((Space)null!);

            // 2. ACT
            var result = await _sut.GetShareListingTimePolicyAsync(10);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
        }

        [Fact]
        public async Task GetShareListingTimePolicyAsync_CurrentUserOwnsSpace_ReturnsUnlockedPolicy()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("owner-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()))
                .ReturnsAsync(new Space { Id = 10, OwnerId = "owner-1" });

            // 2. ACT
            var result = await _sut.GetShareListingTimePolicyAsync(10);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.IsLocked.Should().BeFalse();
        }

        [Fact]
        public async Task GetShareListingTimePolicyAsync_PlatformContractExists_ReturnsLockedPolicy()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()))
                .ReturnsAsync(new Space { Id = 10, OwnerId = "owner-1" });
            _mockUnitOfWork.SetupGet(u => u.contractRepository).Returns(new Mock<IContractRepository>().Object);
            var mockContractRepository = Mock.Get(_mockUnitOfWork.Object.contractRepository);
            mockContractRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new List<Contract>
                {
                    new()
                    {
                        Id = 99,
                        SpaceId = 10,
                        LesseeId = "lessee-1",
                        CanShare = true,
                        Source = ContractSource.Platform,
                        Status = ContractStatusEnum.Active,
                        StartDate = DateTime.Now.AddDays(-1),
                        EndDate = DateTime.Now.AddDays(10),
                        IsActive = true
                    }
                });

            // 2. ACT
            var result = await _sut.GetShareListingTimePolicyAsync(10);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.IsLocked.Should().BeTrue();
            result.Data.ContractId.Should().Be(99);
            result.Data.Source.Should().Be(ContractSource.Platform);
        }

        [Fact]
        public async Task CreateShareListingAsync_WalletSpendFails_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockWalletService
                .Setup(s => s.SpendWalletBalance(50, It.IsAny<string>()))
                .ReturnsAsync(new ServiceResult<WalletRespnse> { IsSuccess = false, Message = "Balance not enough" });

            // 2. ACT
            var result = await _sut.CreateShareListingAsync(CreateSharedListingRequest(), 50, 30);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Balance not enough");
            _mockListingRepository.Verify(r => r.AddAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task UpdateShareListingAsync_ListingNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync((Listing)null!);

            // 2. ACT
            var result = await _sut.UpdateShareListingAsync(99, CreateSharedListingRequest());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockListingRepository.Verify(r => r.UpdateAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task UpdateShareListingAsync_EntireSpaceListing_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(new Listing { Id = 5, ListingType = ListingType.EntireSpace });

            // 2. ACT
            var result = await _sut.UpdateShareListingAsync(5, CreateSharedListingRequest());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("SharedSpace");
            _mockListingRepository.Verify(r => r.UpdateAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task CreateShareListingAsync_ValidOwnerRequest_CreatesSharedListing()
        {
            // 1. ARRANGE
            var request = CreateSharedListingRequest();
            var listing = new Listing { Id = 5, SpaceId = request.SpaceId };
            var availability = new AvailabilitiesTime
            {
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0),
                Specificdate = request.ShareSpaceDetailAvailabilitiesTimes!.First().Specificdate
            };
            var response = CreateShareListingResponse(5, "owner-1");

            SetupWalletSpendSuccess();
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("owner-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync(new Space { Id = request.SpaceId, OwnerId = "owner-1" });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing>());
            _mockMapper.Setup(m => m.Map<Listing>(request)).Returns(listing);
            _mockMapper.Setup(m => m.Map<AvailabilitiesTime>(It.IsAny<AvailabilitiesTimeRequest>())).Returns(availability);
            _mockListingRepository.Setup(r => r.AddAsync(listing)).Returns(Task.CompletedTask);
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(listing);
            _mockMapper.Setup(m => m.Map<ShareListingResponse>(listing)).Returns(response);

            // 2. ACT
            var result = await _sut.CreateShareListingAsync(request, 50, 30);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            listing.CreatorId.Should().Be("owner-1");
            listing.ListingType.Should().Be(ListingType.SharedSpace);
            listing.Status.Should().Be(ListingStatusEnum.Available);
            listing.ShareSpaceDetail.Should().NotBeNull();
            listing.ShareSpaceDetail!.MaxSubRenter.Should().Be(2);
            listing.ShareSpaceDetail.AvailabilitiesTimes.Should().ContainSingle();
            _mockListingRepository.Verify(r => r.AddAsync(listing), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateShareListingAsync_RenterHasShareRight_CreatesSharedListing()
        {
            // 1. ARRANGE
            var request = CreateSharedListingRequest();
            var listing = new Listing { Id = 6, SpaceId = request.SpaceId };
            var response = CreateShareListingResponse(6, "renter-1");

            SetupWalletSpendSuccess();
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("renter-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync(new Space { Id = request.SpaceId, OwnerId = "owner-1" });
            _mockSpaceUsageRightRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<SpaceUsageRight, bool>>>()))
                .ReturnsAsync(new List<SpaceUsageRight>
                {
                    new()
                    {
                        SpaceId = request.SpaceId,
                        UserId = "renter-1",
                        IsActive = true,
                        CanShare = true,
                        Type = SpaceUsageRightType.PrimaryRenter,
                        ValidFrom = request.AllowedStartTime!.Value.ToDateTime(TimeOnly.MinValue).AddDays(-1),
                        ValidTo = request.AllowedEndTime!.Value.ToDateTime(TimeOnly.MinValue).AddDays(1)
                    }
                });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing>());
            _mockMapper.Setup(m => m.Map<Listing>(request)).Returns(listing);
            _mockMapper
                .Setup(m => m.Map<AvailabilitiesTime>(It.IsAny<AvailabilitiesTimeRequest>()))
                .Returns(new AvailabilitiesTime
                {
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    Specificdate = request.ShareSpaceDetailAvailabilitiesTimes!.First().Specificdate
                });
            _mockListingRepository.Setup(r => r.AddAsync(listing)).Returns(Task.CompletedTask);
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(listing);
            _mockMapper.Setup(m => m.Map<ShareListingResponse>(listing)).Returns(response);

            // 2. ACT
            var result = await _sut.CreateShareListingAsync(request, 50, 30);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            listing.CreatorId.Should().Be("renter-1");
            listing.ListingType.Should().Be(ListingType.SharedSpace);
            _mockSpaceUsageRightRepository.Verify(r => r.GetAllAsync(It.IsAny<Expression<Func<SpaceUsageRight, bool>>>()), Times.Once);
            _mockListingRepository.Verify(r => r.AddAsync(listing), Times.Once);
        }

        [Fact]
        public async Task CreateShareListingAsync_SharedTimeOverlapsExistingSharedListing_ReturnsConflict()
        {
            // 1. ARRANGE
            var request = CreateSharedListingRequest();
            var requestTime = request.ShareSpaceDetailAvailabilitiesTimes!.First();
            var existingSharedListing = new Listing { Id = 9, SpaceId = request.SpaceId, ListingType = ListingType.SharedSpace };
            var existingWithTimes = new Listing
            {
                Id = 9,
                SpaceId = request.SpaceId,
                ListingType = ListingType.SharedSpace,
                ShareSpaceDetail = new ShareSpaceDetail
                {
                    AvailabilitiesTimes = new List<AvailabilitiesTime>
                    {
                        new()
                        {
                            StartTime = new TimeOnly(10, 0),
                            EndTime = new TimeOnly(12, 0),
                            Specificdate = requestTime.Specificdate
                        }
                    }
                }
            };

            SetupWalletSpendSuccess();
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("owner-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync(new Space { Id = request.SpaceId, OwnerId = "owner-1" });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing> { existingSharedListing });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(new List<Listing> { existingWithTimes });

            // 2. ACT
            var result = await _sut.CreateShareListingAsync(request, 50, 30);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("#9");
            result.Message.Should().Contain("10:00");
            _mockListingRepository.Verify(r => r.AddAsync(It.IsAny<Listing>()), Times.Never);
        }

        [Fact]
        public async Task UpdateShareListingAsync_ValidRequest_RebuildsSharedDetail()
        {
            // 1. ARRANGE
            var request = CreateSharedListingRequest();
            var existingListing = new Listing
            {
                Id = 5,
                SpaceId = request.SpaceId,
                ListingType = ListingType.SharedSpace,
                ShareSpaceDetail = new ShareSpaceDetail
                {
                    AvailabilitiesTimes = new List<AvailabilitiesTime>(),
                    ShareSpaceAmenities = new List<SharedSpaceAmenities>(),
                    ShareSpaceCategories = new List<ShareSpaceCategory>()
                }
            };
            var response = CreateShareListingResponse(5, "owner-1");

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("owner-1");
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(existingListing);
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync(new Space { Id = request.SpaceId, OwnerId = "owner-1" });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing>());
            _mockMapper.Setup(m => m.Map(request, existingListing)).Returns(existingListing);
            _mockMapper
                .Setup(m => m.Map<AvailabilitiesTime>(It.IsAny<AvailabilitiesTimeRequest>()))
                .Returns(new AvailabilitiesTime { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0), Specificdate = request.ShareSpaceDetailAvailabilitiesTimes!.First().Specificdate });
            _mockMapper.Setup(m => m.Map<ShareListingResponse>(existingListing)).Returns(response);

            // 2. ACT
            var result = await _sut.UpdateShareListingAsync(5, request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            existingListing.ShareSpaceDetail.Should().NotBeNull();
            existingListing.ShareSpaceDetail!.MaxSubRenter.Should().Be(2);
            existingListing.ShareSpaceDetail.AvailabilitiesTimes.Should().ContainSingle();
            _mockListingRepository.Verify(r => r.UpdateAsync(existingListing), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task RenewExpiredListingAsync_ValidRequest_ChargesWalletAndMakesListingAvailable()
        {
            // 1. ARRANGE
            var request = CreateListingRequest();
            var listing = new Listing
            {
                Id = 5,
                SpaceId = request.SpaceId,
                CreatorId = "user-1",
                Status = ListingStatusEnum.Expired,
                IsActive = false,
                IsDeleted = false
            };
            var response = CreateListingResponse(5, "user-1");

            SetupWalletSpendSuccess();
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(listing);
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync(new Space { Id = request.SpaceId, OwnerId = "user-1" });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing>());
            _mockMapper
                .Setup(m => m.Map(request, listing))
                .Returns(listing);
            _mockMapper
                .Setup(m => m.Map<ListingResponse>(listing))
                .Returns(response);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.RenewExpiredListingAsync(5, request, 75, 14);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            listing.Status.Should().Be(ListingStatusEnum.Available);
            listing.IsActive.Should().BeTrue();
            listing.priorityLevel.Should().Be(75);
            listing.durationInDays.Should().Be(14);
            _mockWalletService.Verify(s => s.SpendWalletBalance(75, "Thanh toÃ¡n gia háº¡n bÃ i Ä‘Äƒng"), Times.Once);
            _mockListingRepository.Verify(r => r.UpdateAsync(listing), Times.Once);
        }

        [Fact]
        public async Task DeactivateExpiredListingsAsync_PostingPeriodEnded_MarksListingExpired()
        {
            // 1. ARRANGE
            var expiredListing = new Listing
            {
                Id = 5,
                Status = ListingStatusEnum.Available,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now.AddDays(-3),
                durationInDays = 1
            };
            var activeListing = new Listing
            {
                Id = 6,
                Status = ListingStatusEnum.Available,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                durationInDays = 3
            };

            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing> { expiredListing, activeListing });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.DeactivateExpiredListingsAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(1);
            expiredListing.Status.Should().Be(ListingStatusEnum.Expired);
            expiredListing.IsActive.Should().BeFalse();
            activeListing.Status.Should().Be(ListingStatusEnum.Available);
            activeListing.IsActive.Should().BeTrue();
            _mockListingRepository.Verify(r => r.UpdateAsync(expiredListing), Times.Once);
            _mockBannerRepository.Verify(r => r.UpdateAsync(It.IsAny<Banner>()), Times.Never);
        }

        private static ListingRequest CreateListingRequest() =>
            new()
            {
                SpaceId = 10,
                Name = "Listing",
                Description = "Description",
                Price = 100,
                PriceUnit = PriceUnit.PerDay,
                AllowedStartTime = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                AllowedEndTime = DateOnly.FromDateTime(DateTime.Now.AddDays(10))
            };

        private static SharedListingRequest CreateSharedListingRequest() =>
            new()
            {
                SpaceId = 10,
                Name = "Shared Listing",
                Description = "Description",
                Price = 100,
                PriceUnit = PriceUnit.PerDay,
                AllowedStartTime = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                AllowedEndTime = DateOnly.FromDateTime(DateTime.Now.AddDays(10)),
                ShareSpaceDetailMaxSubRenter = 2,
                ShareSpaceDetailIsLegalCommitted = true,
                ShareSpaceDetailAvailabilitiesTimes = new List<AvailabilitiesTimeRequest>
                {
                    new()
                    {
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(17, 0),
                        Specificdate = DateOnly.FromDateTime(DateTime.Now.AddDays(2))
                    }
                }
            };

        private void SetupWalletSpendSuccess()
        {
            _mockWalletService
                .Setup(s => s.SpendWalletBalance(It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync(new ServiceResult<WalletRespnse>
                {
                    IsSuccess = true,
                    Data = new WalletRespnse { Id = 1, Balance = 100 }
                });
        }

        private static ListingResponse CreateListingResponse(long id, string creatorId = "user-1") =>
            new()
            {
                Id = id,
                CreatorId = creatorId,
                LessorName = "Lessor",
                SpaceAddress = "Address",
                SpaceCity = "City"
            };

        private static ShareListingResponse CreateShareListingResponse(long id, string creatorId = "user-1") =>
            new()
            {
                Id = id,
                CreatorId = creatorId,
                LessorName = "Lessor",
                SpaceAddress = "Address",
                SpaceCity = "City"
            };

        private void SetupListingSortQuery(List<Listing> listings, Action<List<Listing>>? onFiltered = null)
        {
            _mockListingRepository
                .Setup(r => r.GetAllWithSortAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>?>(),
                    It.IsAny<Func<IQueryable<Listing>, IOrderedQueryable<Listing>>?>(),
                    It.IsAny<int?>()))
                .ReturnsAsync((Expression<Func<Listing, bool>> filter,
                    Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>? include,
                    Func<IQueryable<Listing>, IOrderedQueryable<Listing>>? orderBy,
                    int? take) =>
                {
                    var filtered = listings.Where(filter.Compile()).ToList();
                    onFiltered?.Invoke(filtered);
                    return filtered;
                });
        }
    }
}

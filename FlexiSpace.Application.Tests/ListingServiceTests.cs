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
            _mockBannerRepository = new Mock<IBannerRepository>();
            _mockWalletService = new Mock<IWalletService>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCache = new Mock<IDistributedCache>();

            _mockUnitOfWork.SetupGet(u => u.listingRepository).Returns(_mockListingRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.spaceRepository).Returns(_mockSpaceRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.amenityRepository).Returns(_mockAmenityRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.listingReportRepository).Returns(_mockListingReportRepository.Object);
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

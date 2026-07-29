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
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly ListingService _sut;

        public ListingServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockListingRepository = new Mock<IListingRepository>();
            _mockSpaceRepository = new Mock<ISpaceRepository>();
            _mockAmenityRepository = new Mock<IAmentityRepository>();
            _mockListingReportRepository = new Mock<IListingReportRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.SetupGet(u => u.listingRepository).Returns(_mockListingRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.spaceRepository).Returns(_mockSpaceRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.amenityRepository).Returns(_mockAmenityRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.listingReportRepository).Returns(_mockListingReportRepository.Object);

            _sut = new ListingService(_mockUnitOfWork.Object, _mockMapper.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task CreateListingAsync_SpaceNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = CreateListingRequest();
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()))
                .ReturnsAsync((Space)null!);

            // 2. ACT
            var result = await _sut.CreateListingAsync(request);

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
                SpaceAddress = "Address"
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()))
                .ReturnsAsync(new Space { Id = request.SpaceId, OwnerId = "lessor-1" });
            _mockMapper
                .Setup(m => m.Map<Listing>(request))
                .Returns(listing);
            _mockListingRepository
                .Setup(r => r.AddAsync(listing))
                .Returns(Task.CompletedTask);
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
            var result = await _sut.CreateListingAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            listing.CreatorId.Should().Be("lessor-1");
            listing.IsActive.Should().BeTrue();
            listing.Status.Should().Be(ListingStatusEnum.Accepted);
            listing.ListingType.Should().Be(ListingType.EntireSpace);
            _mockListingRepository.Verify(r => r.AddAsync(listing), Times.Once);
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
                .ReturnsAsync(new Listing { Id = 5, Status = ListingStatusEnum.Accepted });

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

        private static ListingRequest CreateListingRequest() =>
            new()
            {
                SpaceId = 10,
                Name = "Listing",
                Description = "Description",
                Price = 100,
                AllowedStartTime = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                AllowedEndTime = DateOnly.FromDateTime(DateTime.Now.AddDays(10))
            };
    }
}

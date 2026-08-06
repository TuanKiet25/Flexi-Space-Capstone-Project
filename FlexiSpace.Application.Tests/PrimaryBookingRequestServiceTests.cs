using AutoMapper;
using FlexiSpace.Application.Events.Bookings;
using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class PrimaryBookingRequestServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IListingRepository> _mockListingRepository;
        private readonly Mock<IPrimaryBookingRequestRepository> _mockBookingRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IPublisher> _mockPublisher;
        private readonly PrimaryBookingRequestService _sut;

        public PrimaryBookingRequestServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockListingRepository = new Mock<IListingRepository>();
            _mockBookingRepository = new Mock<IPrimaryBookingRequestRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockPublisher = new Mock<IPublisher>();

            _mockUnitOfWork.SetupGet(u => u.listingRepository).Returns(_mockListingRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.primaryBookingRequestRepository).Returns(_mockBookingRepository.Object);

            _sut = new PrimaryBookingRequestService(_mockUnitOfWork.Object, _mockMapper.Object, _mockCurrentUserService.Object, _mockPublisher.Object);
        }

        [Fact]
        public async Task CreateBookingRequestAsync_ListingNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = CreateBookingRequest();
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync((Listing)null!);

            // 2. ACT
            var result = await _sut.CreateBookingRequestAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("listing");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task CreateBookingRequestAsync_PastStartDate_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = CreateBookingRequest();
            request.ExpectedStartDate = DateTime.Now.AddDays(-1);
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(new Listing { Id = request.ListingId, CreatorId = "lessor-1", SpaceId = 10 });

            // 2. ACT
            var result = await _sut.CreateBookingRequestAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("quá khứ");
            _mockBookingRepository.Verify(r => r.AddAsync(It.IsAny<PrimaryBookingRequest>()), Times.Never);
        }

        [Fact]
        public async Task CreateBookingRequestAsync_CurrentUserIsLessor_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = CreateBookingRequest();
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(new Listing { Id = request.ListingId, CreatorId = "lessor-1", SpaceId = 10 });
            _mockMapper
                .Setup(m => m.Map<PrimaryBookingRequest>(request))
                .Returns(new PrimaryBookingRequest { Duration = request.Duration, DurationUnit = request.DurationUnit, ExpectedStartDate = request.ExpectedStartDate });

            // 2. ACT
            var result = await _sut.CreateBookingRequestAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("chính mình");
            _mockBookingRepository.Verify(r => r.AddAsync(It.IsAny<PrimaryBookingRequest>()), Times.Never);
        }

        [Fact]
        public async Task CreateBookingRequestAsync_ValidRequest_CreatesPendingBooking()
        {
            // 1. ARRANGE
            var request = CreateBookingRequest();
            var booking = new PrimaryBookingRequest
            {
                Id = 5,
                ListingId = request.ListingId,
                Duration = request.Duration,
                DurationUnit = request.DurationUnit,
                ExpectedStartDate = request.ExpectedStartDate
            };
            var response = new BookingResponse { Id = 5 };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(new Listing { Id = request.ListingId, CreatorId = "lessor-1", SpaceId = 10 });
            _mockMapper
                .Setup(m => m.Map<PrimaryBookingRequest>(request))
                .Returns(booking);
            _mockBookingRepository
                .Setup(r => r.AddAsync(booking))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(booking);
            _mockMapper
                .Setup(m => m.Map<BookingResponse>(booking))
                .Returns(response);

            // 2. ACT
            var result = await _sut.CreateBookingRequestAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            booking.LesseeId.Should().Be("lessee-1");
            booking.LessorId.Should().Be("lessor-1");
            booking.SpaceId.Should().Be(10);
            booking.Status.Should().Be(PrimaryBookingRequestStatusEnum.Pending);
            booking.ExpectedEndDate.Should().Be(request.ExpectedStartDate.AddDays(2));
            _mockBookingRepository.Verify(r => r.AddAsync(booking), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockPublisher.Verify(p => p.Publish(
                It.Is<BookingRequestCreatedEvent>(e =>
                    e.BookingRequestId == booking.Id &&
                    e.ListingId == booking.ListingId &&
                    e.SpaceId == booking.SpaceId &&
                    e.LesseeId == "lessee-1" &&
                    e.LessorId == "lessor-1"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_Approved_PublishesBookingApprovedEvent()
        {
            // 1. ARRANGE
            var booking = new PrimaryBookingRequest
            {
                Id = 5,
                ListingId = 1,
                SpaceId = 10,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                Status = PrimaryBookingRequestStatusEnum.Pending
            };
            var response = new BookingResponse { Id = 5 };
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(booking);
            _mockBookingRepository
                .Setup(r => r.UpdateAsync(booking))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<BookingResponse>(booking))
                .Returns(response);

            // 2. ACT
            var result = await _sut.UpdateStatusAsync(5, new BookingStatusRequest { Status = PrimaryBookingRequestStatusEnum.Approved });

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            booking.Status.Should().Be(PrimaryBookingRequestStatusEnum.Approved);
            _mockPublisher.Verify(p => p.Publish(
                It.Is<BookingRequestApprovedEvent>(e =>
                    e.BookingRequestId == booking.Id &&
                    e.ListingId == booking.ListingId &&
                    e.SpaceId == booking.SpaceId &&
                    e.LessorId == "lessor-1" &&
                    e.LesseeId == "lessee-1"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetBookingRequestByIdAsync_NotFound_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync((PrimaryBookingRequest)null!);

            // 2. ACT
            var result = await _sut.GetBookingRequestByIdAsync(99);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Contain("Không tìm thấy");
        }

        [Fact]
        public async Task UpdateStatusAsync_RejectedWithoutReason_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var booking = new PrimaryBookingRequest { Id = 5, Status = PrimaryBookingRequestStatusEnum.Pending };
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(booking);

            // 2. ACT
            var result = await _sut.UpdateStatusAsync(5, new BookingStatusRequest { Status = PrimaryBookingRequestStatusEnum.Rejected });

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("lý do");
            _mockBookingRepository.Verify(r => r.UpdateAsync(It.IsAny<PrimaryBookingRequest>()), Times.Never);
        }

        [Fact]
        public async Task DeleteBookingRequestAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockBookingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()))
                .ThrowsAsync(new InvalidOperationException("Db failure"));

            // 2. ACT
            var result = await _sut.DeleteBookingRequestAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Db failure");
        }

        private static BookingRequest CreateBookingRequest() =>
            new()
            {
                ListingId = 1,
                Duration = 2,
                DurationUnit = DurationUnitEnum.Days,
                ExpectedStartDate = DateTime.Now.AddDays(1),
                OfferedPrice = 100,
                Purpose = "Office",
                Note = "Need space"
            };
    }
}

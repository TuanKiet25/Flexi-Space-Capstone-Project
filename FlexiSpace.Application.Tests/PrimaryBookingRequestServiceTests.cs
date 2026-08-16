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
        public async Task GetAllBookingRequestsAsync_StatusProvided_ReturnsMappedBookings()
        {
            // 1. ARRANGE
            var bookings = new List<PrimaryBookingRequest>
            {
                new() { Id = 1, Status = PrimaryBookingRequestStatusEnum.Pending },
                new() { Id = 2, Status = PrimaryBookingRequestStatusEnum.Approved }
            };
            var responses = new List<BookingResponse> { new() { Id = 1 } };

            _mockBookingRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>?>()))
                .ReturnsAsync(bookings);
            _mockMapper
                .Setup(m => m.Map<List<BookingResponse>>(bookings))
                .Returns(responses);

            // 2. ACT
            var result = await _sut.GetAllBookingRequestsAsync(PrimaryBookingRequestStatusEnum.Pending);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(responses);
        }

        [Fact]
        public async Task UpdateBookingRequestAsync_PendingBooking_UpdatesBooking()
        {
            // 1. ARRANGE
            var request = CreateBookingRequest();
            request.Duration = 3;
            request.DurationUnit = DurationUnitEnum.Weeks;
            var booking = new PrimaryBookingRequest
            {
                Id = 5,
                Status = PrimaryBookingRequestStatusEnum.Pending,
                ExpectedStartDate = request.ExpectedStartDate,
                Duration = request.Duration,
                DurationUnit = request.DurationUnit
            };
            var response = new BookingResponse { Id = 5 };

            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(booking);
            _mockMapper
                .Setup(m => m.Map(request, booking))
                .Returns(booking);
            _mockMapper
                .Setup(m => m.Map<BookingResponse>(booking))
                .Returns(response);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.UpdateBookingRequestAsync(5, request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            booking.ExpectedEndDate.Should().Be(request.ExpectedStartDate.AddDays(21));
            booking.UpdatedAt.Should().NotBe(default);
            _mockBookingRepository.Verify(r => r.UpdateAsync(booking), Times.Once);
        }

        [Fact]
        public async Task UpdateBookingRequestAsync_NotPending_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(new PrimaryBookingRequest { Id = 5, Status = PrimaryBookingRequestStatusEnum.Approved });

            // 2. ACT
            var result = await _sut.UpdateBookingRequestAsync(5, CreateBookingRequest());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Pending");
            _mockBookingRepository.Verify(r => r.UpdateAsync(It.IsAny<PrimaryBookingRequest>()), Times.Never);
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

        [Fact]
        public async Task CreateBookingRequestAsync_InvalidDuration_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = CreateBookingRequest();
            request.Duration = 0;
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(new Listing { Id = request.ListingId, CreatorId = "lessor-1", SpaceId = 10 });

            // 2. ACT
            var result = await _sut.CreateBookingRequestAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockBookingRepository.Verify(r => r.AddAsync(It.IsAny<PrimaryBookingRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetBookingRequestByIdAsync_Found_ReturnsMappedBooking()
        {
            // 1. ARRANGE
            var booking = new PrimaryBookingRequest { Id = 5 };
            var response = new BookingResponse { Id = 5 };
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(booking);
            _mockMapper.Setup(m => m.Map<BookingResponse>(booking)).Returns(response);

            // 2. ACT
            var result = await _sut.GetBookingRequestByIdAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
        }

        [Fact]
        public async Task UpdateBookingRequestAsync_NotFound_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync((PrimaryBookingRequest)null!);

            // 2. ACT
            var result = await _sut.UpdateBookingRequestAsync(5, CreateBookingRequest());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteBookingRequestAsync_NotFound_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            _mockBookingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()))
                .ReturnsAsync((PrimaryBookingRequest)null!);

            // 2. ACT
            var result = await _sut.DeleteBookingRequestAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteBookingRequestAsync_Found_RemovesBooking()
        {
            // 1. ARRANGE
            _mockBookingRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()))
                .ReturnsAsync(new PrimaryBookingRequest { Id = 5 });

            // 2. ACT
            var result = await _sut.DeleteBookingRequestAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_NotFound_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync((PrimaryBookingRequest)null!);

            // 2. ACT
            var result = await _sut.UpdateStatusAsync(5, new BookingStatusRequest { Status = PrimaryBookingRequestStatusEnum.Approved });

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
        }

        [Theory]
        [InlineData(PrimaryBookingRequestStatusEnum.Rejected)]
        [InlineData(PrimaryBookingRequestStatusEnum.Canceled)]
        public async Task UpdateStatusAsync_RejectedOrCanceledWithReason_UpdatesStatus(PrimaryBookingRequestStatusEnum status)
        {
            // 1. ARRANGE
            var booking = new PrimaryBookingRequest { Id = 5, Status = PrimaryBookingRequestStatusEnum.Pending };
            var response = new BookingResponse { Id = 5 };
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(booking);
            _mockMapper.Setup(m => m.Map<BookingResponse>(booking)).Returns(response);

            // 2. ACT
            var result = await _sut.UpdateStatusAsync(5, new BookingStatusRequest { Status = status, CancelReason = "Busy" });

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            booking.Status.Should().Be(status);
            _mockBookingRepository.Verify(r => r.UpdateAsync(booking), Times.Once);
            _mockPublisher.Verify(p => p.Publish(It.IsAny<BookingRequestApprovedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(DurationUnitEnum.Weeks)]
        [InlineData(DurationUnitEnum.Months)]
        [InlineData(DurationUnitEnum.Years)]
        public async Task CreateBookingRequestAsync_ValidNonDayDurationUnits_CalculatesExpectedEndDate(DurationUnitEnum unit)
        {
            // 1. ARRANGE
            var request = CreateBookingRequest();
            request.DurationUnit = unit;
            request.Duration = 2;
            request.ExpectedStartDate = DateTime.Now.Date.AddDays(30);
            var booking = new PrimaryBookingRequest
            {
                Id = 5,
                ListingId = request.ListingId,
                Duration = request.Duration,
                DurationUnit = request.DurationUnit,
                ExpectedStartDate = request.ExpectedStartDate
            };

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockListingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Listing, bool>>>(),
                    It.IsAny<Func<IQueryable<Listing>, IIncludableQueryable<Listing, object>>>()))
                .ReturnsAsync(new Listing { Id = request.ListingId, CreatorId = "lessor-1", SpaceId = 10 });
            _mockMapper.Setup(m => m.Map<PrimaryBookingRequest>(request)).Returns(booking);
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(booking);

            // 2. ACT
            var result = await _sut.CreateBookingRequestAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            var expectedEndDate = unit switch
            {
                DurationUnitEnum.Weeks => request.ExpectedStartDate.AddDays(14),
                DurationUnitEnum.Months => request.ExpectedStartDate.AddMonths(2),
                DurationUnitEnum.Years => request.ExpectedStartDate.AddYears(2),
                _ => request.ExpectedStartDate
            };
            booking.ExpectedEndDate.Should().Be(expectedEndDate);
        }

        [Theory]
        [InlineData(DurationUnitEnum.Days, 3)]
        [InlineData(DurationUnitEnum.Months, 3)]
        [InlineData(DurationUnitEnum.Years, 3)]
        public async Task UpdateBookingRequestAsync_ValidDurationUnits_CalculatesExpectedEndDate(DurationUnitEnum unit, int duration)
        {
            // 1. ARRANGE
            var request = CreateBookingRequest();
            request.DurationUnit = unit;
            request.Duration = duration;
            request.ExpectedStartDate = DateTime.Now.Date.AddDays(30);
            var booking = new PrimaryBookingRequest
            {
                Id = 5,
                Status = PrimaryBookingRequestStatusEnum.Pending
            };

            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(booking);
            _mockMapper
                .Setup(m => m.Map(request, booking))
                .Callback(() =>
                {
                    booking.Duration = request.Duration;
                    booking.DurationUnit = request.DurationUnit;
                    booking.ExpectedStartDate = request.ExpectedStartDate;
                });

            // 2. ACT
            var result = await _sut.UpdateBookingRequestAsync(5, request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            var expectedEndDate = unit switch
            {
                DurationUnitEnum.Days => request.ExpectedStartDate.AddDays(duration),
                DurationUnitEnum.Months => request.ExpectedStartDate.AddMonths(duration),
                DurationUnitEnum.Years => request.ExpectedStartDate.AddYears(duration),
                _ => request.ExpectedStartDate
            };
            booking.ExpectedEndDate.Should().Be(expectedEndDate);
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

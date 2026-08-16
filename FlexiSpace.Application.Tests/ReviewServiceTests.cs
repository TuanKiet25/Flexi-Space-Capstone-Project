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
using System.Linq.Expressions;

namespace FlexiSpace.Application.Tests
{
    public class ReviewServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IReviewRepository> _mockReviewRepository;
        private readonly Mock<IPrimaryBookingRequestRepository> _mockPrimaryBookingRequestRepository;
        private readonly Mock<IContractRepository> _mockContractRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly ReviewService _sut;

        public ReviewServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockReviewRepository = new Mock<IReviewRepository>();
            _mockPrimaryBookingRequestRepository = new Mock<IPrimaryBookingRequestRepository>();
            _mockContractRepository = new Mock<IContractRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.SetupGet(u => u.reviewRepository).Returns(_mockReviewRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.primaryBookingRequestRepository).Returns(_mockPrimaryBookingRequestRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.contractRepository).Returns(_mockContractRepository.Object);

            _sut = new ReviewService(_mockUnitOfWork.Object, _mockMapper.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReviewsExist_ReturnsMappedReviews()
        {
            // 1. ARRANGE
            var reviews = new List<Review> { new() { Id = 1, ReviewerId = "user-1" } };
            var responses = new List<ReviewResponse> { new() { Id = 1, ReviewerId = "user-1" } };

            _mockReviewRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>?>()))
                .ReturnsAsync(reviews);
            _mockMapper
                .Setup(m => m.Map<IEnumerable<ReviewResponse>>(reviews))
                .Returns(responses);

            // 2. ACT
            var result = await _sut.GetAllAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(responses);
        }

        [Fact]
        public async Task GetByUserIdAsync_MissingUserId_ReturnsFailedResult()
        {
            // 1. ARRANGE

            // 2. ACT
            var result = await _sut.GetByUserIdAsync("");

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("UserId");
            _mockReviewRepository.Verify(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>?>()), Times.Never);
        }

        [Fact]
        public async Task GetBySpaceIdAsync_ReviewsExist_ReturnsMappedReviews()
        {
            // 1. ARRANGE
            var reviews = new List<Review>
            {
                new() { Id = 1, BookingRequestId = 10, PrimaryBookingRequest = new PrimaryBookingRequest { SpaceId = 5 } }
            };
            var responses = new List<ReviewResponse> { new() { Id = 1 } };

            _mockReviewRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>?>()))
                .ReturnsAsync(reviews);
            _mockMapper
                .Setup(m => m.Map<IEnumerable<ReviewResponse>>(reviews))
                .Returns(responses);

            // 2. ACT
            var result = await _sut.GetBySpaceIdAsync(5);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(responses);
        }

        [Fact]
        public async Task CreateAsync_UnauthenticatedUser_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns((string?)null);

            // 2. ACT
            var result = await _sut.CreateAsync(CreateReviewRequestForSpace());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("chưa đăng nhập");
            _mockPrimaryBookingRequestRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NoTargetProvided_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            var request = new ReviewRequest { BookingRequestId = 1, Rating = 5, Description = "Good" };

            // 2. ACT
            var result = await _sut.CreateAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("SpaceId");
        }

        [Fact]
        public async Task CreateAsync_BothSpaceAndTargetProvided_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            var request = CreateReviewRequestForSpace();
            request.TargetUserId = "lessor-1";

            // 2. ACT
            var result = await _sut.CreateAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("chỉ được");
            _mockPrimaryBookingRequestRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_BookingRequestNotFound_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockPrimaryBookingRequestRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()))
                .ReturnsAsync((PrimaryBookingRequest)null!);

            // 2. ACT
            var result = await _sut.CreateAsync(CreateReviewRequestForSpace());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Contain("không tồn tại");
        }

        [Fact]
        public async Task CreateAsync_ValidSpaceReview_CreatesReview()
        {
            // 1. ARRANGE
            var request = CreateReviewRequestForSpace();
            var response = new ReviewResponse { Id = 10, ReviewerId = "lessee-1", Rating = 5 };

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockPrimaryBookingRequestRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()))
                .ReturnsAsync(new PrimaryBookingRequest { Id = 1, SpaceId = 5 });
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract
                {
                    PrimaryBookingRequestId = 1,
                    SpaceId = 5,
                    LesseeId = "lessee-1",
                    LessorId = "lessor-1",
                    Status = ContractStatusEnum.Active
                });
            _mockReviewRepository
                .SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<Review, bool>>>()))
                .ReturnsAsync((Review)null!)
                .ReturnsAsync(new Review { Id = 10, ReviewerId = "lessee-1", Rating = 5 });
            _mockReviewRepository
                .Setup(r => r.AddAsync(It.IsAny<Review>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<ReviewResponse>(It.IsAny<Review>()))
                .Returns(response);

            // 2. ACT
            var result = await _sut.CreateAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            _mockReviewRepository.Verify(r => r.AddAsync(It.Is<Review>(x =>
                x.BookingRequestId == 1 &&
                x.ReviewerId == "lessee-1" &&
                x.TargetUserId == null &&
                x.Rating == 5 &&
                x.IsActive)), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_CurrentUserIsNotContractParticipant_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("other-user");
            _mockPrimaryBookingRequestRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()))
                .ReturnsAsync(new PrimaryBookingRequest { Id = 1, SpaceId = 5 });
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract
                {
                    PrimaryBookingRequestId = 1,
                    SpaceId = 5,
                    LesseeId = "lessee-1",
                    LessorId = "lessor-1",
                    Status = ContractStatusEnum.Active
                });

            // 2. ACT
            var result = await _sut.CreateAsync(CreateReviewRequestForSpace());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không có quyền");
            _mockReviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_DuplicateBookingReview_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockPrimaryBookingRequestRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()))
                .ReturnsAsync(new PrimaryBookingRequest { Id = 1, SpaceId = 5 });
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract
                {
                    PrimaryBookingRequestId = 1,
                    SpaceId = 5,
                    LesseeId = "lessee-1",
                    LessorId = "lessor-1",
                    Status = ContractStatusEnum.Active
                });
            _mockReviewRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Review, bool>>>()))
                .ReturnsAsync(new Review { Id = 99, BookingRequestId = 1 });

            // 2. ACT
            var result = await _sut.CreateAsync(CreateReviewRequestForSpace());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("đã được đánh giá");
            _mockReviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_LesseeReviewsLessor_CreatesUserReview()
        {
            // 1. ARRANGE
            var request = new ReviewRequest { BookingRequestId = 1, TargetUserId = "lessor-1", Rating = 4, Description = "Good host" };
            var response = new ReviewResponse { Id = 11, ReviewerId = "lessee-1", TargetUserId = "lessor-1", Rating = 4 };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockPrimaryBookingRequestRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>()))
                .ReturnsAsync(new PrimaryBookingRequest { Id = 1, SpaceId = 5 });
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract
                {
                    PrimaryBookingRequestId = 1,
                    SpaceId = 5,
                    LesseeId = "lessee-1",
                    LessorId = "lessor-1",
                    Status = ContractStatusEnum.Expired
                });
            _mockReviewRepository
                .SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<Review, bool>>>()))
                .ReturnsAsync((Review)null!)
                .ReturnsAsync(new Review { Id = 11, ReviewerId = "lessee-1", TargetUserId = "lessor-1", Rating = 4 });
            _mockMapper.Setup(m => m.Map<ReviewResponse>(It.IsAny<Review>())).Returns(response);

            // 2. ACT
            var result = await _sut.CreateAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            _mockReviewRepository.Verify(r => r.AddAsync(It.Is<Review>(x =>
                x.TargetUserId == "lessor-1" &&
                x.Name == "Đánh giá người dùng" &&
                x.ReviewerId == "lessee-1")), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReviewBelongsToCurrentUser_SoftDeletesReview()
        {
            // 1. ARRANGE
            var review = new Review { Id = 10, ReviewerId = "user-1", IsDeleted = false };

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockReviewRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Review, bool>>>()))
                .ReturnsAsync(review);
            _mockReviewRepository
                .Setup(r => r.UpdateAsync(review))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.DeleteAsync(10);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            review.IsDeleted.Should().BeTrue();
            review.UpdatedBy.Should().Be("user-1");
            _mockReviewRepository.Verify(r => r.UpdateAsync(review), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReviewNotFound_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockReviewRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Review, bool>>>()))
                .ReturnsAsync((Review)null!);

            // 2. ACT
            var result = await _sut.DeleteAsync(99);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            _mockReviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_CurrentUserDoesNotOwnReview_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("other-user");
            _mockReviewRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Review, bool>>>()))
                .ReturnsAsync(new Review { Id = 10, ReviewerId = "owner-user", IsDeleted = false });

            // 2. ACT
            var result = await _sut.DeleteAsync(10);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("của chính mình");
            _mockReviewRepository.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
        }

        private static ReviewRequest CreateReviewRequestForSpace() =>
            new()
            {
                BookingRequestId = 1,
                SpaceId = 5,
                Rating = 5,
                Description = "Great space"
            };
    }
}

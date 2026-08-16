using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Domain.Entities;
using Moq;
using System.Linq.Expressions;

namespace FlexiSpace.Application.Tests
{
    public class UserAiImageHistoryServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserAiImageHistoryRepository> _mockHistoryRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly UserAiImageHistoryService _sut;

        public UserAiImageHistoryServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockHistoryRepository = new Mock<IUserAiImageHistoryRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.SetupGet(u => u.userAiImageHistoryRepository).Returns(_mockHistoryRepository.Object);

            _sut = new UserAiImageHistoryService(_mockUnitOfWork.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task GetCurrentUserHistoryAsync_MissingUserId_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns(" ");

            // 2. ACT
            var result = await _sut.GetCurrentUserHistoryAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("UserId la bat buoc.");
            _mockHistoryRepository.Verify(r => r.GetAllAsync(It.IsAny<Expression<Func<UserAiImageHistory, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentUserHistoryAsync_ExistingHistories_ReturnsNewestFirst()
        {
            // 1. ARRANGE
            var older = new UserAiImageHistory
            {
                Id = 1,
                UserId = "user-1",
                Prompt = "old",
                ResultImageUrl = "old.png",
                CreatedAt = new DateTime(2026, 1, 1)
            };
            var newer = new UserAiImageHistory
            {
                Id = 2,
                UserId = "user-1",
                Prompt = "new",
                ResultImageUrl = "new.png",
                CreatedAt = new DateTime(2026, 1, 2)
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockHistoryRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<UserAiImageHistory, bool>>>()))
                .ReturnsAsync(new List<UserAiImageHistory> { older, newer });

            // 2. ACT
            var result = await _sut.GetCurrentUserHistoryAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Select(x => x.Id).Should().Equal(2, 1);
            result.Data![0].Prompt.Should().Be("new");
            result.Data![1].ResultImageUrl.Should().Be("old.png");
        }

        [Fact]
        public async Task GetByHistoryIdAsync_HistoryNotFound_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockHistoryRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<UserAiImageHistory, bool>>>()))
                .ReturnsAsync((UserAiImageHistory)null!);

            // 2. ACT
            var result = await _sut.GetByHistoryIdAsync(99);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Be("Khong tim thay lich su AI image voi Id da cho.");
        }

        [Fact]
        public async Task GetByHistoryIdAsync_MissingUserId_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns(" ");

            // 2. ACT
            var result = await _sut.GetByHistoryIdAsync(9);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("UserId la bat buoc.");
            _mockHistoryRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<UserAiImageHistory, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task GetByHistoryIdAsync_ExistingHistory_ReturnsMappedResponse()
        {
            // 1. ARRANGE
            var history = new UserAiImageHistory
            {
                Id = 9,
                UserId = "user-1",
                Prompt = "loft",
                ResultImageUrl = "loft.png",
                CreatedAt = new DateTime(2026, 2, 3)
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockHistoryRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<UserAiImageHistory, bool>>>()))
                .ReturnsAsync(history);

            // 2. ACT
            var result = await _sut.GetByHistoryIdAsync(9);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(9);
            result.Data.Prompt.Should().Be("loft");
            result.Data.ResultImageUrl.Should().Be("loft.png");
            result.Data.CreatedAt.Should().Be(new DateTime(2026, 2, 3));
        }

        [Fact]
        public async Task HardDeleteAsync_HistoryNotFound_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockHistoryRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<UserAiImageHistory, bool>>>()))
                .ReturnsAsync((UserAiImageHistory)null!);

            // 2. ACT
            var result = await _sut.HardDeleteAsync(99);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            _mockHistoryRepository.Verify(r => r.RemoveByIdAsync(It.IsAny<object>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task HardDeleteAsync_ExistingHistory_RemovesHistoryAndSavesChanges()
        {
            // 1. ARRANGE
            var history = new UserAiImageHistory { Id = 9, UserId = "user-1" };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockHistoryRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<UserAiImageHistory, bool>>>()))
                .ReturnsAsync(history);
            _mockHistoryRepository
                .Setup(r => r.RemoveByIdAsync(9L))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.HardDeleteAsync(9);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Xoa lich su AI image thanh cong.");
            _mockHistoryRepository.Verify(r => r.RemoveByIdAsync(9L), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}

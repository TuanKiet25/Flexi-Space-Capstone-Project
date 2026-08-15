using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using Moq;

namespace FlexiSpace.Application.Tests
{
    public class AIToolServiceTests
    {
        private readonly Mock<IFalAiService> _mockFalAiService;
        private readonly Mock<IPictureURL> _mockPictureUrlService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserAiImageHistoryRepository> _mockHistoryRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IWalletService> _mockWalletService;
        private readonly AIToolService _sut;

        public AIToolServiceTests()
        {
            _mockFalAiService = new Mock<IFalAiService>();
            _mockPictureUrlService = new Mock<IPictureURL>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockHistoryRepository = new Mock<IUserAiImageHistoryRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockWalletService = new Mock<IWalletService>();

            _mockUnitOfWork.SetupGet(u => u.userAiImageHistoryRepository).Returns(_mockHistoryRepository.Object);

            _sut = new AIToolService(
                _mockFalAiService.Object,
                _mockPictureUrlService.Object,
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockWalletService.Object);
        }

        [Fact]
        public async Task GenerateImageAsync_ValidRequest_ReturnsCloudinaryUrlAndSavesHistory()
        {
            // 1. ARRANGE
            var request = new GenerateAiImageRequest
            {
                Base64Image = "base64-image",
                Base64Obj = "base64-object",
                Prompt = "modern office"
            };

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockWalletService
                .Setup(s => s.GetOwnWallet())
                .ReturnsAsync(new ServiceResult<WalletRespnse>
                {
                    IsSuccess = true,
                    Data = new WalletRespnse { Balance = 5000 }
                });
            _mockFalAiService
                .Setup(s => s.GenerateInpaintingAsync("base64-image", "modern office", "base64-object"))
                .ReturnsAsync("https://fal.ai/temp.png");
            _mockPictureUrlService
                .Setup(s => s.UploadImageFromUrlAsync("https://fal.ai/temp.png", "flexispace_ai_tools"))
                .ReturnsAsync("https://cloudinary.com/result.png");
            _mockWalletService
                .Setup(s => s.SpendWalletBalance(2000, "Thanh toán sử dụng công cụ AI"))
                .ReturnsAsync(new ServiceResult<WalletRespnse> { IsSuccess = true });
            _mockHistoryRepository
                .Setup(r => r.AddAsync(It.IsAny<UserAiImageHistory>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.GenerateImageAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("https://cloudinary.com/result.png");
            _mockHistoryRepository.Verify(r => r.AddAsync(It.Is<UserAiImageHistory>(h =>
                h.UserId == "user-1" &&
                h.Prompt == "modern office" &&
                h.ResultImageUrl == "https://cloudinary.com/result.png")), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GenerateImageAsync_WalletNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new GenerateAiImageRequest { Base64Image = "image", Prompt = "prompt" };
            _mockWalletService
                .Setup(s => s.GetOwnWallet())
                .ReturnsAsync(new ServiceResult<WalletRespnse> { IsSuccess = false });

            // 2. ACT
            var result = await _sut.GenerateImageAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy ví của bạn hoặc bạn chưa đăng nhập.");
            _mockFalAiService.Verify(s => s.GenerateInpaintingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GenerateImageAsync_InsufficientBalance_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new GenerateAiImageRequest { Base64Image = "image", Prompt = "prompt" };
            _mockWalletService
                .Setup(s => s.GetOwnWallet())
                .ReturnsAsync(new ServiceResult<WalletRespnse>
                {
                    IsSuccess = true,
                    Data = new WalletRespnse { Balance = 1999 }
                });

            // 2. ACT
            var result = await _sut.GenerateImageAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Số dư ví không đủ. Cần tối thiểu 2000 để sử dụng tính năng AI.");
            _mockFalAiService.Verify(s => s.GenerateInpaintingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GenerateImageAsync_AiReturnsEmptyUrl_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = new GenerateAiImageRequest { Base64Image = "image", Prompt = "prompt" };
            _mockWalletService
                .Setup(s => s.GetOwnWallet())
                .ReturnsAsync(new ServiceResult<WalletRespnse>
                {
                    IsSuccess = true,
                    Data = new WalletRespnse { Balance = 5000 }
                });
            _mockFalAiService
                .Setup(s => s.GenerateInpaintingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(string.Empty);

            // 2. ACT
            var result = await _sut.GenerateImageAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Không thể sinh ảnh từ AI.");
            _mockWalletService.Verify(s => s.SpendWalletBalance(It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GenerateImageAsync_SpendWalletFails_ReturnsPaymentFailure()
        {
            // 1. ARRANGE
            var request = new GenerateAiImageRequest { Base64Image = "image", Prompt = "prompt" };
            _mockWalletService
                .Setup(s => s.GetOwnWallet())
                .ReturnsAsync(new ServiceResult<WalletRespnse>
                {
                    IsSuccess = true,
                    Data = new WalletRespnse { Balance = 5000 }
                });
            _mockFalAiService
                .Setup(s => s.GenerateInpaintingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync("https://fal.ai/temp.png");
            _mockPictureUrlService
                .Setup(s => s.UploadImageFromUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("https://cloudinary.com/result.png");
            _mockWalletService
                .Setup(s => s.SpendWalletBalance(It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync(new ServiceResult<WalletRespnse> { IsSuccess = false, Message = "wallet locked" });

            // 2. ACT
            var result = await _sut.GenerateImageAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Đã xảy ra lỗi khi thanh toán: wallet locked");
            _mockHistoryRepository.Verify(r => r.AddAsync(It.IsAny<UserAiImageHistory>()), Times.Never);
        }

        [Fact]
        public async Task GenerateImageAsync_DependencyThrows_ReturnsSystemFailure()
        {
            // 1. ARRANGE
            var request = new GenerateAiImageRequest { Base64Image = "image", Prompt = "prompt" };
            _mockWalletService
                .Setup(s => s.GetOwnWallet())
                .ThrowsAsync(new InvalidOperationException("service unavailable"));

            // 2. ACT
            var result = await _sut.GenerateImageAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Lỗi hệ thống: service unavailable");
        }
    }
}

using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class AIToolService : IAIToolService
    {
        private readonly IFalAiService _falAiService;
        private readonly IPictureURL _pictureUrlService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IWalletService _walletService;
        private static decimal AiDecorCost => 2000;
        public AIToolService(
            IFalAiService falAiService,
            IPictureURL pictureUrlService,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IWalletService walletService)
        {
            _falAiService = falAiService;
            _pictureUrlService = pictureUrlService;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _walletService = walletService;
        }
        public async Task<ServiceResult<string>> GenerateImageAsync(GenerateAiImageRequest request)
        {
            try
            {
                var walletResult = await _walletService.GetOwnWallet();

                if (!walletResult.IsSuccess || walletResult.Data == null)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy ví của bạn hoặc bạn chưa đăng nhập."
                    };
                }

                if (walletResult.Data.Balance < AiDecorCost)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = $"Số dư ví không đủ. Cần tối thiểu {AiDecorCost} để sử dụng tính năng AI."
                    };
                }

                var falTemporaryUrl = await _falAiService.GenerateInpaintingAsync(
                    request.Base64Image,
                    request.Prompt,
                    request.Base64Obj

                );

                if (string.IsNullOrEmpty(falTemporaryUrl))
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "Không thể sinh ảnh từ AI."
                    };
                }
                var cloudinaryUrl = await _pictureUrlService.UploadImageFromUrlAsync(falTemporaryUrl, "flexispace_ai_tools");
                var spendResult = await _walletService.SpendWalletBalance(AiDecorCost, "Thanh toán sử dụng công cụ AI");
                if (!spendResult.IsSuccess)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = $"Đã xảy ra lỗi khi thanh toán: {spendResult.Message}"
                    };
                }
                var historyRecord = new UserAiImageHistory
                {
                    UserId = _currentUserService.UserId,
                    Prompt = request.Prompt,
                    ResultImageUrl = cloudinaryUrl,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.userAiImageHistoryRepository.AddAsync(historyRecord);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<string>
                {
                    IsSuccess = true,
                    Data = cloudinaryUrl
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<string>
                {
                    IsSuccess = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }
    }
}

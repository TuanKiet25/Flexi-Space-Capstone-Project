using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Enum;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<DashboardResponse>> GetDashboardStatsAsync()
        {
            try
            {
                // 1. Calculate total money of all wallets in the system (not deleted)
                var wallets = await _unitOfWork.walletRepository.GetAllAsync(w => !w.IsDeleted);
                decimal totalWalletBalance = wallets.Sum(w => w.Balance);

                // 2. Fetch completed transaction histories related to posting listings or AI image tools
                var histories = await _unitOfWork.transactionHistoryRepository.GetAllAsync(
                    h => !h.IsDeleted && h.Status == TransactionEnum.Completed &&
                         (h.Description == "Thanh toán bài đăng" || h.Description == "Thanh toán sử dụng công cụ AI")
                );

                // Calculate listing spent and count
                var listingTransactions = histories.Where(h => h.Description == "Thanh toán bài đăng").ToList();
                decimal totalListingSpent = listingTransactions.Sum(h => Math.Abs(h.TransactionAmount));
                int totalListingCount = listingTransactions.Count;

                // Calculate AI image tool spent and count
                var aiImageTransactions = histories.Where(h => h.Description == "Thanh toán sử dụng công cụ AI").ToList();
                decimal totalAiImageSpent = aiImageTransactions.Sum(h => Math.Abs(h.TransactionAmount));
                int totalAiImageCount = aiImageTransactions.Count;

                decimal totalSpent = totalListingSpent + totalAiImageSpent;

                var dashboardStats = new DashboardResponse
                {
                    TotalWalletBalance = totalWalletBalance,
                    TotalSpent = totalSpent,
                    TotalListingSpent = totalListingSpent,
                    TotalAiImageSpent = totalAiImageSpent,
                    TotalListingCount = totalListingCount,
                    TotalAiImageCount = totalAiImageCount
                };

                return new ServiceResult<DashboardResponse>
                {
                    IsSuccess = true,
                    Data = dashboardStats,
                    Message = "Lấy dữ liệu dashboard thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<DashboardResponse>
                {
                    IsSuccess = false,
                    Message = $"Lỗi khi tải dữ liệu dashboard: {ex.Message}"
                };
            }
        }
    }
}

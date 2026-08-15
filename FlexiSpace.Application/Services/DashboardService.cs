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
        private readonly ICurrentUserService _currentUserService;

        public DashboardService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
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

        public async Task<ServiceResult<ListingDashboardOverviewResponse>> GetListingOverviewAsync(int rangeDays = 7, int futureDays = 7)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<ListingDashboardOverviewResponse>
                    {
                        IsSuccess = false,
                        Message = "Register first!"
                    };
                }

                rangeDays = Math.Clamp(rangeDays, 1, 90);
                futureDays = Math.Clamp(futureDays, 0, 90);

                var today = DateOnly.FromDateTime(DateTime.Now);
                var periodStart = today.AddDays(-(rangeDays - 1));
                var previousPeriodStart = periodStart.AddDays(-rangeDays);
                var futureEnd = today.AddDays(futureDays);
                var periodStartDateTime = periodStart.ToDateTime(TimeOnly.MinValue);
                var tomorrowDateTime = today.AddDays(1).ToDateTime(TimeOnly.MinValue);

                var listings = await _unitOfWork.listingRepository.GetAllAsync(x =>
                    x.CreatorId == currentUserId &&
                    !x.IsDeleted);

                var listingIds = listings.Select(x => x.Id).ToList();
                var activeListingIds = listings
                    .Where(x => !x.IsDeleted && x.IsActive && x.Status == ListingStatusEnum.Available)
                    .Select(x => x.Id)
                    .ToList();

                var viewStats = await _unitOfWork.listingViewDailyStatRepository.GetAllAsync(x =>
                    activeListingIds.Contains(x.ListingId) &&
                    x.Date >= periodStart &&
                    x.Date <= today);

                var bookingRequests = await _unitOfWork.primaryBookingRequestRepository.GetAllAsync(x =>
                    activeListingIds.Contains(x.ListingId) &&
                    x.LessorId == currentUserId &&
                    !x.IsDeleted &&
                    x.CreatedAt >= periodStartDateTime &&
                    x.CreatedAt < tomorrowDateTime);

                var contracts = await _unitOfWork.contractRepository.GetAllAsync(x =>
                    x.LessorId == currentUserId &&
                    !x.IsDeleted &&
                    x.Status == ContractStatusEnum.Active);

                var totalThisPeriod = listings.Count(x => ToDateOnly(x.CreatedAt) >= periodStart && ToDateOnly(x.CreatedAt) <= today);
                var totalPreviousPeriod = listings.Count(x => ToDateOnly(x.CreatedAt) >= previousPeriodStart && ToDateOnly(x.CreatedAt) < periodStart);

                var response = new ListingDashboardOverviewResponse
                {
                    TotalListings = listings.Count,
                    TotalListingsDelta = totalThisPeriod - totalPreviousPeriod,
                    ActiveListings = listings.Count(x => x.IsActive && x.Status == ListingStatusEnum.Available),
                    SignedContracts = contracts.Count,
                    ExpiredListings = listings.Count(x => x.Status == ListingStatusEnum.Expired),
                    ActiveListingViewsLastPeriod = viewStats.Sum(x => x.ViewCount),
                    ActiveListingBookingRequestsLastPeriod = bookingRequests.Count,
                    NewListingsTrend = BuildDateRange(periodStart, today)
                        .Select(date => new DashboardChartPointResponse
                        {
                            Label = FormatDate(date),
                            Value = listings.Count(x => ToDateOnly(x.CreatedAt) == date)
                        })
                        .ToList(),
                    ActiveInteractionTrend = BuildDateRange(periodStart, today)
                        .Select(date => new DashboardInteractionChartPointResponse
                        {
                            Label = FormatDate(date),
                            Views = viewStats.Where(x => x.Date == date).Sum(x => x.ViewCount),
                            BookingRequests = bookingRequests.Count(x => ToDateOnly(x.CreatedAt) == date)
                        })
                        .ToList(),
                    SignedContractsTrend = BuildMonthRange(DateTime.Now.AddMonths(-5), DateTime.Now)
                        .Select(month => new DashboardChartPointResponse
                        {
                            Label = month.ToString("yyyy-MM"),
                            Value = contracts.Count(x => x.CreatedAt.Year == month.Year && x.CreatedAt.Month == month.Month)
                        })
                        .ToList(),
                    ExpiredTrend = BuildDateRange(periodStart, futureEnd)
                        .Select(date => new DashboardExpiringChartPointResponse
                        {
                            Label = FormatDate(date),
                            Value = listings.Count(x => GetListingExpiredDate(x) == date),
                            IsFuture = date > today
                        })
                        .ToList()
                };

                return new ServiceResult<ListingDashboardOverviewResponse>
                {
                    IsSuccess = true,
                    Data = response,
                    Message = "Lấy dữ liệu dashboard listing thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ListingDashboardOverviewResponse>
                {
                    IsSuccess = false,
                    Message = $"Lỗi khi tải dữ liệu dashboard listing: {ex.Message}"
                };
            }
        }

        private static IEnumerable<DateOnly> BuildDateRange(DateOnly start, DateOnly end)
        {
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                yield return date;
            }
        }

        private static IEnumerable<DateTime> BuildMonthRange(DateTime start, DateTime end)
        {
            var month = new DateTime(start.Year, start.Month, 1);
            var lastMonth = new DateTime(end.Year, end.Month, 1);

            while (month <= lastMonth)
            {
                yield return month;
                month = month.AddMonths(1);
            }
        }

        private static DateOnly ToDateOnly(DateTime dateTime) => DateOnly.FromDateTime(dateTime);

        private static DateOnly GetListingExpiredDate(Domain.Entities.Listing listing) =>
            DateOnly.FromDateTime(listing.CreatedAt.AddDays(listing.durationInDays));

        private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");
    }
}

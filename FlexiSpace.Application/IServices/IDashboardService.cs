using FlexiSpace.Application.ViewModels.Responses;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IDashboardService
    {
        Task<ServiceResult<DashboardResponse>> GetDashboardStatsAsync();
        Task<ServiceResult<ListingDashboardOverviewResponse>> GetListingOverviewAsync(int rangeDays = 7, int futureDays = 7);
    }
}

using FlexiSpace.Application.ViewModels.Responses;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IDashboardService
    {
        Task<ServiceResult<DashboardResponse>> GetDashboardStatsAsync();
    }
}

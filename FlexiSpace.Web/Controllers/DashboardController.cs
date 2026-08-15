using FlexiSpace.Application.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlexiSpace.Web.Controllers
{
    public class DashboardController : MyBaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var result = await _dashboardService.GetDashboardStatsAsync();
            return HandleResult(result);
        }

        [HttpGet("listing-overview")]
        public async Task<IActionResult> GetListingOverview([FromQuery] int rangeDays = 7, [FromQuery] int futureDays = 7)
        {
            var result = await _dashboardService.GetListingOverviewAsync(rangeDays, futureDays);
            return HandleResult(result);
        }
    }
}

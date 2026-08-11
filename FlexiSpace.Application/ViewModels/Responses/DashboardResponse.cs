using System;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class DashboardResponse
    {
        public decimal TotalWalletBalance { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalListingSpent { get; set; }
        public decimal TotalAiImageSpent { get; set; }
        public int TotalListingCount { get; set; }
        public int TotalAiImageCount { get; set; }
    }
}

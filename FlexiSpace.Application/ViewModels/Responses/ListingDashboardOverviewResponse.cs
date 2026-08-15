namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ListingDashboardOverviewResponse
    {
        public int TotalListings { get; set; }
        public int TotalListingsDelta { get; set; }
        public int ActiveListings { get; set; }
        public int SignedContracts { get; set; }
        public int ExpiredListings { get; set; }
        public int ActiveListingViewsLastPeriod { get; set; }
        public int ActiveListingBookingRequestsLastPeriod { get; set; }
        public List<DashboardChartPointResponse> NewListingsTrend { get; set; } = new();
        public List<DashboardInteractionChartPointResponse> ActiveInteractionTrend { get; set; } = new();
        public List<DashboardChartPointResponse> SignedContractsTrend { get; set; } = new();
        public List<DashboardExpiringChartPointResponse> ExpiredTrend { get; set; } = new();
    }

    public class DashboardChartPointResponse
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class DashboardInteractionChartPointResponse
    {
        public string Label { get; set; } = string.Empty;
        public int Views { get; set; }
        public int BookingRequests { get; set; }
    }

    public class DashboardExpiringChartPointResponse
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsFuture { get; set; }
    }
}

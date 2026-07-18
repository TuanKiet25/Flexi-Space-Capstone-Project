namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ListingReportDetailResponse
    {
        public long ListingId { get; set; }
        public int TotalReportCount { get; set; }
        public List<ReportReasonCountResponse> ReasonBreakdown { get; set; } = new();
    }

    public class ReportReasonCountResponse
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

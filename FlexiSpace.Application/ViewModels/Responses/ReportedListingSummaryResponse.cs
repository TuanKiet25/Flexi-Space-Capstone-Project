namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ReportedListingSummaryResponse
    {
        public long ListingId { get; set; }
        public int ReportCount { get; set; }
        public bool IsBanned { get; set; }
        public string ListingStatus { get; set; } = string.Empty;
        public string ListingDescription { get; set; } = string.Empty;
    }
}

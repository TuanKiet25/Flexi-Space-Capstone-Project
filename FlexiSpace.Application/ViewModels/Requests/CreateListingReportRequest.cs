using FlexiSpace.Domain.Enum;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class CreateListingReportRequest
    {
        public long ListingId { get; set; }
        public List<ReportReasonEnum> Reasons { get; set; } = new();
        public string? AdditionalDetails { get; set; }
    }
}

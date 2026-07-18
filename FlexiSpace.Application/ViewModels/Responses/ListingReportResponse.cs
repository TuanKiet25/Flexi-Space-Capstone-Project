using FlexiSpace.Domain.Enum;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ListingReportResponse
    {
        public long Id { get; set; }
        public long ListingId { get; set; }
        public string ReporterId { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public List<ReportReasonEnum> Reasons { get; set; } = new();
        public string AdditionalDetails { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using FlexiSpace.Domain.Enum;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ShareListingTimePolicyResponse
    {
        public bool IsLocked { get; set; }
        public long? ContractId { get; set; }
        public ContractSource? Source { get; set; }
        public DateOnly? AllowedStartTime { get; set; }
        public DateOnly? AllowedEndTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

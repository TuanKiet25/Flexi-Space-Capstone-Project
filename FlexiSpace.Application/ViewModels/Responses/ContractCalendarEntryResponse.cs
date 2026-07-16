using System;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ContractCalendarEntryResponse
    {
        public DateTime EffectiveDate { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public long ContractId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string BusinessDescription { get; set; } = string.Empty;
        public string DisplayLabel { get; set; } = string.Empty;
    }
}

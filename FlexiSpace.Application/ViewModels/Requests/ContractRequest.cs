using FlexiSpace.Domain.Enum;
using System;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class ContractRequest
    {
        required
        public string ConversationId { get; set; } 
        public long SpaceId { get; set; }
        public long PrimaryBookingRequestId { get; set; }

        public DurationUnitEnum DurationUnit { get; set; }
        public int Duration { get; set; } 
        public DateTime StartDate { get; set; } 

        public decimal Acreage { get; set; }
        public decimal Price { get; set; } 
        public decimal? DepositAmount { get; set; } 
        public string? Description { get; set; }
        public List<ContractScheduleRequest>? ContractSchedules { get; set; }
    }
}
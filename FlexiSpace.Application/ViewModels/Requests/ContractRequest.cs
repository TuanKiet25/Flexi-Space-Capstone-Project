using FlexiSpace.Domain.Enum;
using System;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class ContractRequest
    {
        public long SpaceId { get; set; }
        public long PrimaryBookingRequestId { get; set; }
        public string? LessorNumberCard { get; set; }
        public string? LesseeNumberCard { get; set; }
        public string? Description { get; set; }
        public decimal Acreage { get; set; }
        public int Duration { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal Price { get; set; }
        public ContractStatusEnum Status { get; set; }
    }
}
using FlexiSpace.Domain.Enum;
using System;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class SubleaseContractInfoResponse
    {
        public bool HasContract { get; set; }
        public long? ContractId { get; set; }
        public long ListingId { get; set; }
        public long SpaceId { get; set; }
        public string? LessorId { get; set; }
        public string? LessorName { get; set; }
        public string? LesseeId { get; set; }
        public string? LesseeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool CanShare { get; set; }
        public decimal Acreage { get; set; }
        public ContractStatusEnum? Status { get; set; }
        public ContractSource? Source { get; set; }
        public string? Message { get; set; }
    }
}

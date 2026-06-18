using FlexiSpace.Domain.Enum;
using System;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ContractResponse
    {
        public long Id { get; set; }
        public string? LessorId { get; set; }
        public string? LesseeId { get; set; }
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
    }
}
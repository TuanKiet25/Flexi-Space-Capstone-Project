using System;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ReviewResponse
    {
        public long Id { get; set; }
        public long BookingRequestId { get; set; }
        public string ReviewerId { get; set; }
        public string ReviewerName { get; set; }
        public string? TargetUserId { get; set; }
        public string? TargetUserName { get; set; }
        public long? SpaceId { get; set; }
        public string? SpaceAddress { get; set; }
        public int Rating { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

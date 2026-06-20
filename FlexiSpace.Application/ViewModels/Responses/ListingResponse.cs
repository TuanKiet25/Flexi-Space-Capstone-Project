using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ListingResponse
    {
        public long Id { get; set; }
        public long SpaceId { get; set; }
        required
        public string CreatorId { get; set; }
        public DateTime AllowedStartTime { get; set; }
        public DateTime AllowedEndTime { get; set; }
        public string? Description { get; set; }
        public ListingType ListingType { get; set; }
        public ListingStatusEnum Status { get; set; }
        required
        public string LessorName { get; set; }
        required
        public string SpaceAddress
        { get; set; }
        public DateTime CreatedAt { get; set; } 
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } 
        public bool IsActive { get; set; }
        public string? CancelReason { get; set; }
        public List<string>? ListingPictures { get; set; }

    }
}

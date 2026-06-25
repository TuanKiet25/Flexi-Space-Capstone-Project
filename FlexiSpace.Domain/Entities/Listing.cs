using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Listing : BaseEntity
    {
        public Listing()
        {
            PrimaryBookingRequests = new HashSet<PrimaryBookingRequest>();
            PictureURLs = new HashSet<PictureURL>();
        }

        public long Id { get; set; }
        public long SpaceId { get; set; }
        public string CreatorId { get; set; }
        public DateOnly AllowedStartTime { get; set; }
        public DateOnly AllowedEndTime { get; set; }
        public string Description { get; set; }
        public string CancelReason { get; set; }
        public decimal Price { get; set; }
        public ListingType ListingType { get; set; }
        public ListingStatusEnum Status { get; set; }
        public List<string> ListingPictures { get; set; }
        public virtual ShareSpaceDetail ShareSpaceDetail { get; set; }
        public virtual User Lessor { get; set; }
        public virtual Space Space { get; set; }
        public virtual ICollection<PrimaryBookingRequest> PrimaryBookingRequests { get; set; }
        public virtual ICollection<PictureURL> PictureURLs { get; set; }
    }
}

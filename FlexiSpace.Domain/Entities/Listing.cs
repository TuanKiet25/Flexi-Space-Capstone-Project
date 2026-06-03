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
        }

        public long Id { get; set; }
        public long SpaceId { get; set; }
        public long PrimaryBookingRequestId { get; set; }
        public string CreatorId { get; set; }
        public DateTime AllowedStartTime { get; set; }
        public DateTime AllowedEndTime { get; set; }
        public decimal HourlyRate { get; set; }
        public string Description { get; set; }
        public ListingStatusEnum Status { get; set; }

        public virtual User Lessor { get; set; }
        public virtual Space Space { get; set; }
        public virtual ListingSlot ListingSlot { get; set; }
        public virtual BussinessCategory BussinessCategory { get; set; }
        public virtual ICollection<PrimaryBookingRequest> PrimaryBookingRequests { get; set; }
    }
}

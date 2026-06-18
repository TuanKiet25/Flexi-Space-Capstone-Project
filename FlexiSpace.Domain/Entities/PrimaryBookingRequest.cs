using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class PrimaryBookingRequest : BaseEntity
    {
        public PrimaryBookingRequest()
        {
            SubBookingRequests = new HashSet<SubBookingRequest>();
            Contracts = new HashSet<Contract>();
        }

        public long Id { get; set; }
        public long SpaceId { get; set; }
        public long ListingId { get; set; }
        public string LessorId { get; set; }
        public string LesseeId { get; set; }
        public decimal? OfferedPrice { get; set; }
        public int Duration { get; set; }
        public DurationUnitEnum DurationUnit { get; set; }
        public DateTime ExpectedStartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public string Purpose { get; set; } 
        public string Note { get; set; }
        public PrimaryBookingRequestStatusEnum Status { get; set; }
        public virtual User Lessee { get; set; }
        public virtual User Lessor { get; set; }
        public virtual Space Space { get; set; }
        public virtual ICollection<SubBookingRequest> SubBookingRequests { get; set; }
        public virtual ICollection<Contract> Contracts { get; set; }
        public virtual Listing Listing { get; set; }
        public virtual Review Review { get; set; }
    }
}

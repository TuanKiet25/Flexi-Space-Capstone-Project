using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Review : BaseEntity
    {
        public Review() 
        { 
            SubBookingRequest = new HashSet<SubBookingRequest>();
            PrimaryBookingRequest = new HashSet<PrimaryBookingRequest>();
        }

        public long Id { get; set; }
        public long SubBookingRequestId { get; set; }
        public long BookingRequestId { get; set; }
        public string ReviewerId { get; set; }
        public string TargetUserId { get; set; }
        public int Rating { get; set; }
        public string Description { get; set; }

        public virtual User Reviewer { get; set; }
        public virtual User TargetUser { get; set; }
        public virtual ICollection<SubBookingRequest> SubBookingRequest { get; set; }
        public virtual ICollection<PrimaryBookingRequest> PrimaryBookingRequest { get; set; }

    }
}

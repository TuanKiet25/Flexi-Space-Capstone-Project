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

        public long Id { get; set; }
        public required string SpaceId { get; set; }
        public required string LessorId { get; set; }
        public required string LesseeId { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public PrimaryBookingRequestStatusEnum Status { get; set; }

        public virtual User Lessor { get; set; }
        public virtual Space Space { get; set; }
        public virtual SubBookingRequest SubBookingRequest { get; set; }
        public virtual Listing Listing { get; set; }
        public virtual Review Review { get; set; }
    }
}

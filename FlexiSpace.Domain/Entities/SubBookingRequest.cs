using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class SubBookingRequest : BaseEntity
    {
        public long Id { get; set; }
        public string LessorId { get; set; }
        public string LesseeId { get; set; }
        public decimal Price { get; set; }
        public EnumSubBookingRequestStatus Status { get; set; }

        public virtual User Lessor { get; set; }
        public virtual PrimaryBookingRequest PrimaryBookingRequest { get; set; }
        public virtual Review Review { get; set; }
    }
}

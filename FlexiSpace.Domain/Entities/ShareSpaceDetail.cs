using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class ShareSpaceDetail
    {
        public long ListingId { get; set; }
        public int MaxSubRenter { get; set; }
        public virtual ICollection<AvailabilitiesTime> AvailabilitiesTimes { get; set; }
        public virtual ICollection<SharedSpaceAmenities> ShareSpaceAmenities { get; set; }
        public virtual ICollection<ShareSpaceCategory> ShareSpaceCategories { get; set; }
        public virtual Listing Listing { get; set; }
        
    }
}

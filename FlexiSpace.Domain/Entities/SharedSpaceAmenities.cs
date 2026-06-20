using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class SharedSpaceAmenities
    {
        public long Id { get; set; }
        public long AmenityId { get; set; }
        public long ShareSpaceDetailId { get; set; }
        public bool IsIncluded { get; set; }
        public decimal Price { get; set; }
        public virtual Amentity Amenity { get; set; }
        public ShareSpaceDetail ShareSpaceDetail { get; set; }
    }
}

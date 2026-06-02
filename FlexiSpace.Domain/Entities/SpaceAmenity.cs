using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class SpaceAmenity
    {
        public string SpaceId { get; set; }
        public long AmenityId { get; set; }
        public virtual Space Space { get; set; }
        public virtual Amentity Amenity { get; set; }
    }
}

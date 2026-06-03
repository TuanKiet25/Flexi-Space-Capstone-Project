using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Space : BaseEntity
    {
        public Space()
        {
            PrimaryBookingRequest = new HashSet<PrimaryBookingRequest>();
            Listing = new HashSet<Listing>();
            SpaceAmenity = new HashSet<SpaceAmenity>();
            OperatingHour = new HashSet<OperatingHour>();
            SpaceAllowedCategory = new HashSet<SpaceAllowedCategory>();
        }

        public long Id { get; set; }
        public string OwnerId { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public decimal Area { get; set; }
        public string SpacePictures { get; set; }

        public virtual User Owner { get; set; }
        public virtual ICollection<PrimaryBookingRequest> PrimaryBookingRequest { get; set; }
        public virtual ICollection<Listing> Listing { get; set; }
        public virtual ICollection<SpaceAmenity> SpaceAmenity { get; set; }
        public virtual ICollection<OperatingHour> OperatingHour { get; set; }
        public virtual ICollection<SpaceAllowedCategory> SpaceAllowedCategory { get; set; }
    }
}

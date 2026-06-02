using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class BussinessCategory : BaseEntity
    {
        public BussinessCategory()
        {
            SpaceAllowedCategories = new HashSet<SpaceAllowedCategory>();
            Listings = new HashSet<Listing>();
        }

        public long Id { get; set; }

        public virtual ICollection<SpaceAllowedCategory> SpaceAllowedCategories { get; set; }
        public virtual ICollection<Listing> Listings { get; set; }
    }
}

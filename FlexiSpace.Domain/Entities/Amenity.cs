using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Amentity : BaseEntity
    {
        public Amentity()
        {
            SpaceAmenity = new HashSet<SpaceAmenity>();
        }

        public long Id { get; set; }
        public int Number { get; set; }

        public virtual ICollection<SpaceAmenity> SpaceAmenity { get; set; }
    }
}

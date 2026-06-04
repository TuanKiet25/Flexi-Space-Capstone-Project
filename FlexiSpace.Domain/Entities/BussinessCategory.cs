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
        }

        public long Id { get; set; }

        public virtual ICollection<SpaceAllowedCategory> SpaceAllowedCategories { get; set; }
    }
}

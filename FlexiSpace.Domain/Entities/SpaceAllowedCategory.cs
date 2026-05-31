using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class SpaceAllowedCategory
    {
        public string SpaceId { get; set; }
        public long BussinessCategoryId { get; set; }
        public virtual Space Space { get; set; }
        public virtual BussinessCategory BussinessCategory { get; set; }
    }
}

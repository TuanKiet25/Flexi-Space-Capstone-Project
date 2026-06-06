using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Amentity : BaseEntity
    {
        public long Id { get; set; }
        public long SpaceId { get; set; }
        public int Quantity { get; set; }

        public virtual Space Space { get; set; }
    }
}

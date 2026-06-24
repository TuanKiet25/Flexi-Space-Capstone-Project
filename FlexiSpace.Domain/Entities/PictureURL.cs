using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class PictureURL : BaseEntity
    {
        public long Id { get; set; }
        public string ImageUrl { get; set; }
        public string PublicId { get; set; }
        public bool IsPrimary { get; set; }
        public long? SpaceId { get; set; }

        public virtual Space Space { get; set; }
    }
}

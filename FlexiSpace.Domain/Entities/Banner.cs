using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Banner : BaseEntity
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int DurationInDays { get; set; }
        public long? ListingId { get; set; }

        public virtual Listing Listing { get; set; }
        public virtual PictureURL PictureURL { get; set; }
    }
}

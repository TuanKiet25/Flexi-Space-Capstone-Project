using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class ShareSpaceCategory : BaseEntity
    {
        public long Id { get; set; }
        public long ShareSpaceDetailId { get; set; }
        public string Note { get; set; }
        public virtual ShareSpaceDetail ShareSpaceDetail { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class ShareSpaceCategory
    {
        public long Id { get; set; }
        public long BussinessCategoryId { get; set; }
        public long ShareSpaceDetailId { get; set; }
        public string Note { get; set; }
        public virtual ShareSpaceDetail ShareSpaceDetail { get; set; }  
        public virtual BussinessCategory BusinessCategory { get; set; }

    }
}

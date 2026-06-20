using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class ShareSpaceAmenitiesRequest
    {
        public long AmenityId { get; set; }
        public bool IsIncluded { get; set; }
        public decimal Price { get; set; }
    }
}

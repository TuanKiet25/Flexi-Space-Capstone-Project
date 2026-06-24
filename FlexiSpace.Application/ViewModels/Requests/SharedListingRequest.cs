using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class SharedListingRequest : ListingRequest
    {

        public int ShareSpaceDetailMaxSubRenter { get; set; }
        public bool ShareSpaceDetailIsOwner { get; set; }
        public bool ShareSpaceDetailIsLegalCommitted { get; set; }
        public List<ShareSpaceAmenitiesRequest>? ShareSpaceDetailShareSpaceAmenities { get; set; }
        public List<AvailabilitiesTimeRequest>? ShareSpaceDetailAvailabilitiesTimes { get; set; }
        public List<ShareSpaceCategoryRequest>? ShareSpaceDetailShareSpaceCategories { get; set; }
    }
}

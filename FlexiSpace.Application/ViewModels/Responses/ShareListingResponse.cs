using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ShareListingResponse : ListingResponse
    {
     
        public int ShareSpaceDetailMaxSubRenter { get; set; }
        public bool ShareSpaceDetailIsOwner { get; set; }
        public bool ShareSpaceDetailIsLegalCommitted { get; set; }
        public DateTime ShareSpaceDetailLegalCommittedAt { get; set; }
        public List<ShareSpaceAmenitiesResponse>? ShareSpaceDetailShareSpaceAmenities { get; set; }
        public List<AvailabilitiesResponse>? ShareSpaceDetailAvailabilitiesTimes { get; set; }
        public List<ShareSpaceCategoryResponse>? ShareSpaceDetailShareSpaceCategories { get; set; }
    }
}

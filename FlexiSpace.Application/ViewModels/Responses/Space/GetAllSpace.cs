using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses.Space
{
    public class GetAllSpace : BaseVModel
    {
        public long Id { get; set; }
        public string? OwnerId { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public decimal Area { get; set; }
        public bool IsDeleted { get; set; } 

        //public List<PrimaryBookingRequest>? PrimaryBookingRequests { get; set; }
        //public List<Listing>? Listings { get; set; }
        public List<AmenityVModel>? Amenities { get; set; }
        public List<OperatingHourVmodel>? OperatingHours { get; set; }
        public List<SpaceAllowedCategoryVModel>? SpaceAllowedCategories { get; set; }
        public List<PictureURLVModel>? PictureURLs { get; set; }
    }
}

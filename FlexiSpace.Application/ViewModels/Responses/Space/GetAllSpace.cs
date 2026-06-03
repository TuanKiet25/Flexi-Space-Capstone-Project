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
        public string? SpacePictures { get; set; }

        public User? Owner { get; set; }
        public List<PrimaryBookingRequest>? PrimaryBookingRequests { get; set; }
        public List<Listing>? Listings { get; set; }
        public List<SpaceAmenity>? SpaceAmenities { get; set; }
        public List<OperatingHour>? OperatingHours { get; set; }
        public List<SpaceAllowedCategory>? SpaceAllowedCategories { get; set; }
    }
}

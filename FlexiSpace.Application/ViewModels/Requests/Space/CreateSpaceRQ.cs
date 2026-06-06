using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests.Space
{
    public class CreateSpaceRQ
    {
        
        public string? Name { get; set; }
        [JsonIgnore]
        public string? OwnerId { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public decimal Area { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public List<AmenityVModel>? Amenities { get; set; }
        public List<OperatingHourVmodel>? OperatingHours { get; set; }
        public List<SpaceAllowedCategoryVModel>? SpaceAllowedCategories { get; set; }
    }

    public class AmenityVModel
    {
        public string? Name { get; set; }
        public int? Quantity { get; set; }
        public bool? IsActive { get; set; }
        [JsonIgnore]
        public DateTime? CreatedBy { get; set; }
    }

    public class OperatingHourVmodel
    {
        public int DayOfWeek { get; set; }
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
    }

    public class SpaceAllowedCategoryVModel
    {
        public long? BussinessCategoryId { get; set; }
    }
}

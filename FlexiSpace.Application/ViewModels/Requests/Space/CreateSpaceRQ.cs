using FlexiSpace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
        [BindNever]
        [JsonIgnore]
        public long? Id { get; set; }
        public string? Name { get; set; }
        [BindNever]
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
        public List<IFormFile>? PictureURLs { get; set; }
    }

    public class CreateSpaceRP : CreateSpaceRQ
    {
        public new long Id { get; set; }
        public new string? OwnerId { get; set; }
    }

    public class UpdateSpaceRQ : CreateSpaceRQ
    {
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

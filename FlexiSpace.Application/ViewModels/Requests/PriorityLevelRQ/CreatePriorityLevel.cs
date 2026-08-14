using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests.PriorityLevelRQ
{
    public class CreatePriorityLevel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int DurationForBanner { get; set; }
        public PriorityLevelTypeEnum Type { get; set; }
        public bool? IsActive { get; set; }
        [JsonIgnore]
        public string? CreatedBy { get; set; }
    }

    public class GetAllPriorityLevel : BaseVModel
    {
        public long Id { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int DurationForBanner { get; set; }
        public PriorityLevelTypeEnum Type { get; set; }
        public bool IsActive { get; set; }
    }

    public class FilterGetAllPriorityLevel : BaseVModel
    {
    }
}

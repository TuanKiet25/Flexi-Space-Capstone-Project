using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class PriorityLevel : BaseEntity
    {
        public long Id { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int durationInDays { get; set; }
        public int DurationForBanner { get; set; }
        public PriorityLevelTypeEnum Type { get; set; }
    }
}

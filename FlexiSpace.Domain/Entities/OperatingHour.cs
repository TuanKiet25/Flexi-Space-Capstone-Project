using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class OperatingHour : BaseEntity
    {
        public long Id { get; set; }
        public long SpaceId { get; set; }
        public int DayOfWeek { get; set; }
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }

        public virtual Space Space { get; set; }
    }
}

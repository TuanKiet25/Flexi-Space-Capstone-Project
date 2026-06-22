using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class AvailabilitiesTime
    {
        public long Id { get; set; }
        public long ShareSpaceDetailId { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();
        public DateOnly Specificdate { get; set; } // ngày đặt biệt thuê chính rảnh ngày đó muốn share lại space
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly ValidTo { get; set; }
        public ShareSpaceDetail ShareSpaceDetail { get; set; }
    }
}

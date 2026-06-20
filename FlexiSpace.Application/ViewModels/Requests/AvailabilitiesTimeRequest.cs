using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class AvailabilitiesTimeRequest
    {
        public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();
        public DateOnly? Specificdate { get; set; } 
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
    }
}

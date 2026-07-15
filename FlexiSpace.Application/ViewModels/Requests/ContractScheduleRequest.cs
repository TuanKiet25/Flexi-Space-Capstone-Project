using System;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class ContractScheduleRequest
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}

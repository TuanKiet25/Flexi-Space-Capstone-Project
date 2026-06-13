using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class BookingRequest
    {
        public long ListingId { get; set; }
        public decimal? OfferedPrice { get; set; }
        public int Duration { get; set; }
        public DurationUnitEnum DurationUnit { get; set; }
        public string? Purpose { get; set; }
        public string? Note { get; set; }
        public DateTime ExpectedStartDate { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class BookingResponse
    {
        public long Id { get; set; }
        public long SpaceId { get; set; }
        public long ListingId { get; set; }
        public string? LessorId { get; set; }
        public string? LessorName { get; set; }
        public string? LesseeId { get; set; }
        public string? LesseeName { get; set; }
        public decimal? OfferedPrice { get; set; }
        public int Duration { get; set; }
        public DateTime ExpectedStartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public string? Purpose { get; set; }
        public string? Note { get; set; }
    }
}

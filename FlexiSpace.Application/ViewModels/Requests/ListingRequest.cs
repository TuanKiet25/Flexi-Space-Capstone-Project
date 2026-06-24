using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class ListingRequest
    {
        public long SpaceId { get; set; }
        public DateOnly? AllowedStartTime { get; set; }
        public DateOnly? AllowedEndTime { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public List<string>? ListingPictures { get; set; }
    }
}

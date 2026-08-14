using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class CreateBannerRequest
    {
        public string? Title { get; set; }

        public string? Description { get; set; }

        public long? ListingId { get; set; }
    }

    public class UpdateBannerRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}

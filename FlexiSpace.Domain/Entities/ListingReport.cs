using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class ListingReport
    {
        public long Id { get; set; }

        public long ListingId { get; set; }
        public virtual Listing Listing { get; set; }

        public string ReporterId { get; set; }
        public virtual User Reporter { get; set; }

        public string ReasonType { get; set; } = string.Empty;

        public string AdditionalDetails { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

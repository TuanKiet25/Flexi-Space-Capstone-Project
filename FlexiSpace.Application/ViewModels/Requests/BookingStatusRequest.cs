using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class BookingStatusRequest
    {
        public PrimaryBookingRequestStatusEnum Status { get; set; }
        public string? CancelReason { get; set; }
    }
}

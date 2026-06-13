using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IPrimaryBookingRequestService
    {
        Task<ServiceResult<BookingResponse>> CreateBookingRequestAsync(BookingRequest request);
        Task<ServiceResult<List<BookingResponse>>> GetAllBookingRequestsAsync(PrimaryBookingRequestStatusEnum? status);
        Task<ServiceResult<BookingResponse>> GetBookingRequestByIdAsync(long id);
        Task<ServiceResult<BookingResponse>> UpdateBookingRequestAsync(long id, BookingRequest request);
        Task<ServiceResult<BookingResponse>> DeleteBookingRequestAsync(long id);
        Task<ServiceResult<BookingResponse>> UpdateStatusAsync(long id, BookingStatusRequest request);
    }
}

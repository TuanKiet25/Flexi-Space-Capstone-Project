using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Application.Services
{
    public class PrimaryBookingRequestService : IPrimaryBookingRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public PrimaryBookingRequestService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<BookingResponse>> CreateBookingRequestAsync(BookingRequest request)
        {
            try
            {
                var listing = await _unitOfWork.listingRepository.GetAsync(
                    x => x.Id == request.ListingId && !x.IsDeleted && x.IsActive,
                    include: q => q.Include(l => l.Space));

                if (listing == null)
                {
                    return new ServiceResult<BookingResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy listing với Id đã cho hoặc listing không hoạt động."
                    };
                }

                if (request.ExpectedStartDate.Date < DateTime.Now.Date)
                {
                    return new ServiceResult<BookingResponse>
                    {
                        IsSuccess = false,
                        Message = "Thời gian bắt đầu không thể là thời gian trong quá khứ."
                    };
                }

                if (request.Duration <= 0)
                {
                    return new ServiceResult<BookingResponse>
                    {
                        IsSuccess = false,
                        Message = "Thời lượng phải lớn hơn 0."
                    };
                }

                var newBooking = _mapper.Map<PrimaryBookingRequest>(request);
                newBooking.ExpectedEndDate = request.DurationUnit switch
                {
                    DurationUnitEnum.Days   => request.ExpectedStartDate.AddDays(request.Duration),
                    DurationUnitEnum.Weeks  => request.ExpectedStartDate.AddDays(request.Duration * 7),
                    DurationUnitEnum.Months => request.ExpectedStartDate.AddMonths(request.Duration),
                    DurationUnitEnum.Years  => request.ExpectedStartDate.AddYears(request.Duration),
                    _ => throw new ArgumentOutOfRangeException(nameof(request.DurationUnit), "Đơn vị thời gian không hợp lệ.")
                };
                newBooking.LesseeId = _currentUserService.UserId;
                newBooking.LessorId = listing.CreatorId;
                if(newBooking.LesseeId == newBooking.LessorId)
                {
                    return new ServiceResult<BookingResponse>
                    {
                        IsSuccess = false,
                        Message = "Bạn không thể đặt chỗ cho chính mình."
                    };
                }
                newBooking.SpaceId = listing.SpaceId;
                newBooking.Status = PrimaryBookingRequestStatusEnum.Pending;
                newBooking.CreatedAt = DateTime.Now;

                await _unitOfWork.primaryBookingRequestRepository.AddAsync(newBooking);
                await _unitOfWork.SaveChangesAsync();

                var result = await _unitOfWork.primaryBookingRequestRepository.GetAsync(
                    x => x.Id == newBooking.Id,
                    include: q => q.Include(b => b.Lessor).Include(b => b.Space).Include(b => b.Listing).Include(b => b.Lessee));

                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<BookingResponse>(result)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<List<BookingResponse>>> GetAllBookingRequestsAsync(PrimaryBookingRequestStatusEnum? status)
        {
            try
            {
                var bookings = await _unitOfWork.primaryBookingRequestRepository.GetAllAsync(
                    b => !b.IsDeleted && (status == null || b.Status == status),
                    include: q => q.Include(b => b.Lessor).Include(b => b.Space).Include(b => b.Listing).Include(b => b.Lessee));

                return new ServiceResult<List<BookingResponse>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<BookingResponse>>(bookings)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<BookingResponse>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<BookingResponse>> GetBookingRequestByIdAsync(long id)
        {
            try
            {
                var booking = await _unitOfWork.primaryBookingRequestRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(b => b.Lessor).Include(b => b.Space).Include(b => b.Listing).Include(b => b.Lessee));

                if (booking == null)
                {
                    return new ServiceResult<BookingResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy yêu cầu đặt chỗ với Id đã cho."
                    };
                }

                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<BookingResponse>(booking)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<BookingResponse>> UpdateBookingRequestAsync(long id, BookingRequest request)
        {
            try
            {
                var existing = await _unitOfWork.primaryBookingRequestRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(b => b.Lessor).Include(b => b.Space).Include(b => b.Listing).Include(b => b.Lessee));

                if (existing == null)
                {
                    return new ServiceResult<BookingResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy yêu cầu đặt chỗ với Id đã cho."
                    };
                }

                if (existing.Status != PrimaryBookingRequestStatusEnum.Pending )
                {
                    return new ServiceResult<BookingResponse>
                    {
                        IsSuccess = false,
                        Message = "Chỉ có thể cập nhật yêu cầu ở trạng thái Pending."

                    };
                }

                _mapper.Map(request, existing);

                existing.ExpectedEndDate = existing.DurationUnit switch
                {
                    DurationUnitEnum.Days   => existing.ExpectedStartDate.AddDays(existing.Duration),
                    DurationUnitEnum.Weeks  => existing.ExpectedStartDate.AddDays(existing.Duration * 7),
                    DurationUnitEnum.Months => existing.ExpectedStartDate.AddMonths(existing.Duration),
                    DurationUnitEnum.Years  => existing.ExpectedStartDate.AddYears(existing.Duration),
                    _ => throw new ArgumentOutOfRangeException(nameof(existing.DurationUnit), "Đơn vị thời gian không hợp lệ.")
                };
                existing.UpdatedAt = DateTime.Now;

                await _unitOfWork.primaryBookingRequestRepository.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<BookingResponse>(existing)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<BookingResponse>> DeleteBookingRequestAsync(long id)
        {
            try
            {
                var existing = await _unitOfWork.primaryBookingRequestRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted);

                if (existing == null)
                {
                    return new ServiceResult<BookingResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy yêu cầu đặt chỗ với Id đã cho."
                    };
                }

                await _unitOfWork.primaryBookingRequestRepository.RemoveByIdAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = true,
                    Message = "Xóa yêu cầu đặt chỗ thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<BookingResponse>> UpdateStatusAsync(long id, BookingStatusRequest request)
        {
            try
            {
                var booking = await _unitOfWork.primaryBookingRequestRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(b => b.Lessor).Include(b => b.Space).Include(b => b.Listing).Include(b => b.Lessee));

                if (booking == null)
                {
                    return new ServiceResult<BookingResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy yêu cầu đặt chỗ với Id đã cho."
                    };
                }

                if (request.Status == PrimaryBookingRequestStatusEnum.Rejected ||
                    request.Status == PrimaryBookingRequestStatusEnum.Canceled)
                {
                    if (string.IsNullOrWhiteSpace(request.CancelReason))
                    {
                        return new ServiceResult<BookingResponse>
                        {
                            IsSuccess = false,
                            Message = "Vui lòng cung cấp lý do từ chối/hủy."
                        };
                    }
                }

                booking.Status = request.Status;
                booking.UpdatedAt = DateTime.Now;

                await _unitOfWork.primaryBookingRequestRepository.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<BookingResponse>(booking)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<BookingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
    }
}

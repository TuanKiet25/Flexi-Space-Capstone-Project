using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class ListingService : IListingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        public ListingService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }
        private string? ValidationMessage(ListingRequest listing)
        {
           var isExistedSpace = _unitOfWork.spaceRepository.GetAsync(x => x.Id == listing.SpaceId);
            if(isExistedSpace == null)
            {
                return "Không tìm thấy không gian với Id đã cho.";
            }
            DateTime currentTime = DateTime.Now;
           if(listing.AllowedStartTime.Date < currentTime.Date || listing.AllowedEndTime.Date < currentTime.Date)
            {
                return "Thời gian bắt đầu và kết thúc không thể là thời gian trong quá khứ.";
            }
           if(listing.AllowedStartTime.Date >= listing.AllowedEndTime.Date)
            {
                return "Thời bắt đầu thúc hợp đồng không thể là vượt quá thời gian kết thúc";
            }
           if(listing.AllowedEndTime.Date <= listing.AllowedStartTime.Date)
            {
                return " thời gian kết thúc hợp đồng không thể trước thời gian bắt đầu ";
            }
           if(listing.Price <= 0)
            {
                return "Giá không thể là số âm và phải có giá trị lớn hơn.";
            }
           
            return null;
        }
        public async Task<ServiceResult<ListingResponse>> CreateListingAsync(ListingRequest listing)
        {
            try
            {
                var checkValidation = ValidationMessage(listing);
                if (checkValidation != null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = checkValidation
                    };
                }
                var newListing = _mapper.Map<Listing>(listing);
                newListing.CreatorId = _currentUserService.UserId;
                newListing.CreatedAt = DateTime.Now;
                newListing.IsActive = false;
                newListing.Status = Domain.Enum.ListingStatusEnum.Pending;
                await _unitOfWork.listingRepository.AddAsync(newListing);
                await _unitOfWork.SaveChangesAsync();
                var listingResult = await _unitOfWork.listingRepository.GetAsync(x => x.Id == newListing.Id, include: q => q.Include(l => l.Space).Include(l => l.Lessor));
                var result = _mapper.Map<ListingResponse>(listingResult);
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = true,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };

            }
        }
    
        public async Task<ServiceResult<ListingResponse>> GetListingByIdAsync(long id)
        {
            try
            {
                var listing = await _unitOfWork.listingRepository.GetAsync(
                    x => x.Id == id,
                    include: q => q.Include(l => l.Space).Include(l => l.Lessor));
                if (listing == null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy listing với Id đã cho."
                    };
                }
                var result = _mapper.Map<ListingResponse>(listing);
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = true,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ListingResponse>> UpdateListingAsync(long id, ListingRequest listing)
        {
            try
            {
                var existingListing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == id, include: q => q.Include(l => l.Space).Include(l => l.Lessor));
                if (existingListing == null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy listing với Id đã cho."
                    };
                }
                var checkValidation = ValidationMessage(listing);
                if (checkValidation != null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = checkValidation
                    };
                }
                _mapper.Map(listing, existingListing);
                existingListing.UpdatedAt = DateTime.Now;
                await _unitOfWork.listingRepository.UpdateAsync(existingListing);
                await _unitOfWork.SaveChangesAsync();
                var result = _mapper.Map<ListingResponse>(existingListing);
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = true,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ListingResponse>> HardDeleteListingAsync(long id)
        {
            try
            {
                var existingListing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == id);
                if (existingListing == null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy listing với Id đã cho."
                    };
                }
                await _unitOfWork.listingRepository.RemoveByIdAsync(id);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = true,
                    Message = "Xóa listing thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<List<ListingResponse>>> GetAllListingsAsync(ListingStatusEnum? status)
        {
            try
            {
                var listings = await _unitOfWork.listingRepository.GetAllAsync(
                    l => !l.IsDeleted
                        && (status == null || l.Status == status),
                    include: q => q.Include(l => l.Space).Include(l => l.Lessor));
                return new ServiceResult<List<ListingResponse>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<ListingResponse>>(listings)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<ListingResponse>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ListingResponse>> AcceptOrCancelListingAsync(long id, ListingStatusRequest request)
        {
            try
            {
                var listing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (listing == null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy listing với Id đã cho."
                    };
                }
                if (request.Status == Domain.Enum.ListingStatusEnum.Accepted)
                {
                    listing.Status = Domain.Enum.ListingStatusEnum.Accepted;
                    listing.IsActive = true;
                    listing.CacelReason = null;
                }
                else if (request.Status == Domain.Enum.ListingStatusEnum.Canceled)
                {
                    if (string.IsNullOrWhiteSpace(request.CancelReason))
                    {
                        return new ServiceResult<ListingResponse>
                        {
                            IsSuccess = false,
                            Message = "Vui lòng cung cấp lý do hủy."
                        };
                    }
                    listing.Status = Domain.Enum.ListingStatusEnum.Canceled;
                    listing.IsActive = false;
                    listing.CacelReason = request.CancelReason;
                }
                else
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Trạng thái không hợp lệ. Chỉ chấp nhận Accepted hoặc Canceled."
                    };
                }
                await _unitOfWork.listingRepository.UpdateAsync(listing);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ListingResponse>(listing)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ListingResponse>> SoftDeleteListingAsync(long id)
        {
            try
            {
                var listing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (listing == null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy listing với Id đã cho."
                    };
                }
                listing.IsDeleted = true;
                await _unitOfWork.listingRepository.UpdateAsync(listing);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = true,
                    Message = "Xóa listing thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

    }
}

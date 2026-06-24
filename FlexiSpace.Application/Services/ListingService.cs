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
using System.Reflection;
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
        private async Task<string?> ValidationMessageAsync(ListingRequest listing)
        {
            var isExistedSpace = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == listing.SpaceId);
            if (isExistedSpace == null)
            {
                return "Không tìm thấy mặt bằng với Id đã cho.";
            }
           
            DateOnly currentTime = DateOnly.FromDateTime(DateTime.Now);
            if (listing.AllowedStartTime != null)
            {
                if (listing.AllowedStartTime < currentTime )
                {
                    return "Thời gian bắt đầu không thể nằm trong quá khứ.";
                }
                if (listing.AllowedStartTime >= listing.AllowedEndTime)
                {
                    return "Thời gian bắt đầu hợp đồng phải diễn ra trước thời gian kết thúc.";
                }
            }
            if(listing.AllowedEndTime != null)
            {
                if(listing.AllowedEndTime < currentTime )
                {
                    return "Thời gian kết thúc không thể nằm trong quá khứ.";
                }
            }
            if(listing.AllowedStartTime != null && listing.AllowedEndTime != null)
            {
               if(listing.AllowedStartTime >= listing.AllowedEndTime)
                {
                    return "Thời gian bắt đầu hợp đồng phải diễn ra trước thời gian kết thúc.";
                }
            }

            if (listing.Price <= 0)
            {
                return "Giá cho thuê phải lớn hơn 0.";
            }
            if (listing is SharedListingRequest sharedListing)
            {
                if (sharedListing.ShareSpaceDetailMaxSubRenter <= 0)
                {
                    return "Số lượng người thuê phụ tối đa phải lớn hơn 0.";
                }

                if (sharedListing.ShareSpaceDetailAvailabilitiesTimes == null || !sharedListing.ShareSpaceDetailAvailabilitiesTimes.Any())
                {
                    return "Vui lòng thiết lập ít nhất một khung giờ cho thuê.";
                }
                if(sharedListing.ShareSpaceDetailIsOwner == false)
                {
                    if(listing.AllowedEndTime == null || listing.AllowedStartTime == null)
                    {
                        return "Vui lòng thiết lập thời gian bắt đầu và kết thúc hợp đồng chính khi bạn không phải là chủ sở hữu.";
                    }
                }
                foreach (var time in sharedListing.ShareSpaceDetailAvailabilitiesTimes)
                {
                    // Kiểm tra ValidFrom và ValidTo của bản thân khung giờ
                    if (time.ValidFrom != null && time.ValidTo != null && time.ValidFrom > time.ValidTo)
                    {
                        return "Trong khung giờ cho thuê, ngày bắt đầu (ValidFrom) không được lớn hơn ngày kết thúc (ValidTo).";
                    }

                    // Kiểm tra ValidFrom phải >= AllowedStartTime
                    if (listing.AllowedStartTime != null && time.ValidFrom != null)
                    {
                        if (time.ValidFrom < listing.AllowedStartTime)
                        {
                            return $"Ngày bắt đầu khung giờ ({time.ValidFrom}) không được sớm hơn thời gian bắt đầu hợp đồng chính ({listing.AllowedStartTime}).";
                        }
                    }

                    // Kiểm tra ValidTo phải <= AllowedEndTime
                    if (listing.AllowedEndTime != null && time.ValidTo != null)
                    {
                        if (time.ValidTo > listing.AllowedEndTime)
                        {
                            return $"Ngày kết thúc khung giờ ({time.ValidTo}) không được trễ hơn thời gian cho phép của hợp đồng ({listing.AllowedEndTime}).";
                        }
                    }
                }

            }

            return null;
        }
        public async Task<ServiceResult<ListingResponse>> CreateListingAsync(ListingRequest listing)
        {
            try
            {
                var checkValidation = await ValidationMessageAsync(listing);
                if (checkValidation != null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = checkValidation.ToString()
                    };
                }
                var newListing = _mapper.Map<Listing>(listing);
                newListing.CreatorId = _currentUserService.UserId;
                newListing.CreatedAt = DateTime.Now;
                newListing.IsActive = false;
                newListing.Status = Domain.Enum.ListingStatusEnum.Pending;
                newListing.ListingType = ListingType.EntireSpace;
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
                var checkValidation = await ValidationMessageAsync(listing);
                if (checkValidation != null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = checkValidation.ToString()
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

        public async Task<ServiceResult<List<ShareListingResponse>>> GetAllListingsAsync(ListingStatusEnum? status, ListingType? listingType = null)
        {
            try
            {
                var listings = await _unitOfWork.listingRepository.GetAllAsync(
                    l => !l.IsDeleted
                        && (status == null || l.Status == status)
                        && (listingType == null || l.ListingType == listingType),
                    include: q => q.Include(l => l.Space)
                                    .Include(l => l.Lessor)
                                    .Include(l => l.ShareSpaceDetail)
                                        .ThenInclude(s => s.AvailabilitiesTimes)
                                    .Include(l => l.ShareSpaceDetail)
                                        .ThenInclude(s => s.ShareSpaceAmenities)
                                    .Include(l => l.ShareSpaceDetail)
                                        .ThenInclude(s => s.ShareSpaceCategories));

                var mappedListings = _mapper.Map<List<ShareListingResponse>>(listings);

                return new ServiceResult<List<ShareListingResponse>>
                {
                    IsSuccess = true,
                    Data = mappedListings
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<ShareListingResponse>>
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
                    listing.CancelReason = null;
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
                    listing.CancelReason = request.CancelReason;
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
        public async Task<ServiceResult<ShareListingResponse>> CreateShareListingAsync(SharedListingRequest sharedListingRequest)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var checkValidation = await ValidationMessageAsync(sharedListingRequest);
                if (checkValidation != null)
                {
                    return new ServiceResult<ShareListingResponse>
                    {
                        IsSuccess = false,
                        Message = checkValidation.ToString()
                    };
                }
                //Validation cơ sở vật chất có trong space không
                if (sharedListingRequest.ShareSpaceDetailShareSpaceAmenities != null &&
                sharedListingRequest.ShareSpaceDetailShareSpaceAmenities.Any())
                {
                    var requestedAmenityIds = sharedListingRequest.ShareSpaceDetailShareSpaceAmenities
                        .Select(x => x.AmenityId)
                        .Distinct()
                        .ToList();
                    var validSpaceAmenities = await _unitOfWork.amenityRepository
                        .GetAllAsync(x => x.SpaceId == sharedListingRequest.SpaceId);
                    var validAmenityIds = validSpaceAmenities.Select(x => x.Id).ToList();
                    var invalidIds = requestedAmenityIds.Except(validAmenityIds).ToList();

                    if (invalidIds.Any())
                    {

                        return new ServiceResult<ShareListingResponse>
                        {
                            IsSuccess = false,
                            Message = $"Lỗi bảo mật: Các tiện ích có ID [{string.Join(", ", invalidIds)}] không tồn tại hoặc không thuộc về Mặt bằng (Space) hiện tại!"
                        };
                    }
                }
                if(sharedListingRequest.ShareSpaceDetailIsLegalCommitted == false)
                {
                    return new ServiceResult<ShareListingResponse>
                    {
                        IsSuccess = false,
                        Message = $"Tích vào vô xác nhận các thỏa thuận"
                    };
                }
                var newListing = _mapper.Map<Listing>(sharedListingRequest);
                newListing.CreatorId = _currentUserService.UserId;
                newListing.CreatedAt = DateTime.Now;
                newListing.IsActive = false;
                newListing.Status = Domain.Enum.ListingStatusEnum.Pending;
                newListing.ListingType = ListingType.SharedSpace;
                newListing.ShareSpaceDetail = new ShareSpaceDetail
                {
                    MaxSubRenter = sharedListingRequest.ShareSpaceDetailMaxSubRenter,
                    IsLegalCommitted = sharedListingRequest.ShareSpaceDetailIsLegalCommitted,
                    LegalCommittedAt =DateTime.Now,
                    AvailabilitiesTimes = sharedListingRequest.ShareSpaceDetailAvailabilitiesTimes?
                        .Select(x => _mapper.Map<AvailabilitiesTime>(x))
                        .ToList() ?? new List<AvailabilitiesTime>(),
                    ShareSpaceAmenities = sharedListingRequest.ShareSpaceDetailShareSpaceAmenities?
                        .Select(x => _mapper.Map<SharedSpaceAmenities>(x))
                        .ToList() ?? new List<SharedSpaceAmenities>(),
                    ShareSpaceCategories = sharedListingRequest.ShareSpaceDetailShareSpaceCategories?
                        .Select(x => _mapper.Map<ShareSpaceCategory>(x))
                        .ToList() ?? new List<ShareSpaceCategory>()
                };
                await _unitOfWork.listingRepository.AddAsync(newListing);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                var listingResult = await _unitOfWork.listingRepository.GetAsync(x => x.Id == newListing.Id,
                    include: q => q.Include(l => l.Space)
                                    .Include(l => l.Lessor)
                                    .Include(l => l.ShareSpaceDetail)
                                    .ThenInclude(l => l.AvailabilitiesTimes)
                                    .Include(l => l.ShareSpaceDetail)
                                    .ThenInclude(l => l.ShareSpaceAmenities)
                                    .Include(l => l.ShareSpaceDetail)
                                    .ThenInclude(l => l.ShareSpaceCategories));
                                    
                var result = _mapper.Map<ShareListingResponse>(listingResult);

                return new ServiceResult<ShareListingResponse>
                {
                    IsSuccess = true,
                    Data = result
                };
            }
            catch(Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<ShareListingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ShareListingResponse>> UpdateShareListingAsync(long id, SharedListingRequest sharedListingRequest)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingListing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(l => l.Space)
                                    .Include(l => l.Lessor)
                                    .Include(l => l.ShareSpaceDetail)
                                    .ThenInclude(l => l.AvailabilitiesTimes)
                                    .Include(l => l.ShareSpaceDetail)
                                    .ThenInclude(l => l.ShareSpaceAmenities)
                                    .Include(l => l.ShareSpaceDetail)
                                    .ThenInclude(l => l.ShareSpaceCategories));

                if (existingListing == null)
                {
                    return new ServiceResult<ShareListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy listing với Id đã cho."
                    };
                }

                if (existingListing.ListingType != ListingType.SharedSpace)
                {
                    return new ServiceResult<ShareListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Listing này không phải là loại SharedSpace."
                    };
                }

                var checkValidation = await ValidationMessageAsync(sharedListingRequest);
                if (checkValidation != null)
                {
                    return new ServiceResult<ShareListingResponse>
                    {
                        IsSuccess = false,
                        Message = checkValidation.ToString()
                    };
                }
                //Validation cơ sở vật chất có trong space không
                if (sharedListingRequest.ShareSpaceDetailShareSpaceAmenities != null &&
                sharedListingRequest.ShareSpaceDetailShareSpaceAmenities.Any())
                {
                    var requestedAmenityIds = sharedListingRequest.ShareSpaceDetailShareSpaceAmenities
                        .Select(x => x.AmenityId)
                        .Distinct()
                        .ToList();
                    var validSpaceAmenities = await _unitOfWork.amenityRepository
                        .GetAllAsync(x => x.SpaceId == sharedListingRequest.SpaceId);
                    var validAmenityIds = validSpaceAmenities.Select(x => x.Id).ToList();
                    var invalidIds = requestedAmenityIds.Except(validAmenityIds).ToList();

                    if (invalidIds.Any())
                    {

                        return new ServiceResult<ShareListingResponse>
                        {
                            IsSuccess = false,
                            Message = $"Lỗi bảo mật: Các tiện ích có ID [{string.Join(", ", invalidIds)}] không tồn tại hoặc không thuộc về Mặt bằng (Space) hiện tại!"
                        };
                    }
                }
                _mapper.Map(sharedListingRequest, existingListing);
                existingListing.UpdatedAt = DateTime.Now;

                if (existingListing.ShareSpaceDetail == null)
                {
                    existingListing.ShareSpaceDetail = new ShareSpaceDetail();
                }

                existingListing.ShareSpaceDetail.MaxSubRenter = sharedListingRequest.ShareSpaceDetailMaxSubRenter;
                existingListing.ShareSpaceDetail.AvailabilitiesTimes = sharedListingRequest.ShareSpaceDetailAvailabilitiesTimes?
                    .Select(x => _mapper.Map<AvailabilitiesTime>(x))
                    .ToList() ?? new List<AvailabilitiesTime>();
                existingListing.ShareSpaceDetail.ShareSpaceAmenities = sharedListingRequest.ShareSpaceDetailShareSpaceAmenities?
                    .Select(x => _mapper.Map<SharedSpaceAmenities>(x))
                    .ToList() ?? new List<SharedSpaceAmenities>();
                existingListing.ShareSpaceDetail.ShareSpaceCategories = sharedListingRequest.ShareSpaceDetailShareSpaceCategories?
                    .Select(x => _mapper.Map<ShareSpaceCategory>(x))
                    .ToList() ?? new List<ShareSpaceCategory>();

                await _unitOfWork.listingRepository.UpdateAsync(existingListing);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var listingResult = await _unitOfWork.listingRepository.GetAsync(x => x.Id == id,
                    include: q => q.Include(l => l.Space)
                                    .Include(l => l.Lessor)
                                    .Include(l => l.ShareSpaceDetail)
                                    .ThenInclude(l => l.AvailabilitiesTimes)
                                    .Include(l => l.ShareSpaceDetail)
                                    .ThenInclude(l => l.ShareSpaceAmenities)
                                    .Include(l => l.ShareSpaceDetail)
                                    .ThenInclude(l => l.ShareSpaceCategories));

                var result = _mapper.Map<ShareListingResponse>(listingResult);

                return new ServiceResult<ShareListingResponse>
                {
                    IsSuccess = true,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<ShareListingResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
        
    }
}

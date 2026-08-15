using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class ListingService : IListingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletService _walletService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDistributedCache _cache;
        private static readonly DistributedCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        public ListingService(IUnitOfWork unitOfWork, IWalletService walletService, IMapper mapper, ICurrentUserService currentUserService, IDistributedCache cache)

        {
            _unitOfWork = unitOfWork;
            _walletService = walletService;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _cache = cache;
        }

        private static string GetListingCacheKey(ListingStatusEnum? status, ListingType? listingType)
        {
            string strStatus = status?.ToString() ?? "All";
            string strType = listingType?.ToString() ?? "All";
            return $"Cache:Listings:Status_{strStatus}:Type_{strType}";
        }

        private async Task<T?> GetCacheAsync<T>(string cacheKey)
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            return string.IsNullOrEmpty(cachedData)
                ? default
                : JsonSerializer.Deserialize<T>(cachedData);
        }

        private Task SetCacheAsync<T>(string cacheKey, T data)
        {
            return _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(data), CacheOptions);
        }

        private async Task InvalidateListingCacheAsync(long? listingId = null)
        {
            var keysToInvalidate = new List<string>();

            var allStatuses = Enum.GetValues<ListingStatusEnum>().Cast<ListingStatusEnum?>().ToList();
            allStatuses.Add(null); 

            var allTypes = Enum.GetValues<ListingType>().Cast<ListingType?>().ToList();
            allTypes.Add(null); 
            foreach (var status in allStatuses)
            {
                foreach (var type in allTypes)
                {
                    keysToInvalidate.Add(GetListingCacheKey(status, type));
                }
            }
            keysToInvalidate.Add("Cache:ListingReports:ReportedListings");

            if (listingId.HasValue)
            {
                keysToInvalidate.Add($"Cache:Listing:Id_{listingId.Value}");
                keysToInvalidate.Add($"Cache:ListingReports:Listing_{listingId.Value}");
                keysToInvalidate.Add($"Cache:ListingReports:Detail_{listingId.Value}");
            }

            foreach (var key in keysToInvalidate.Distinct())
            {
                try
                {
                    await _cache.RemoveAsync(key);
                }
                catch
                {
                }
            }
        }

        private async Task<string?> ValidationMessageAsync(ListingRequest listing, long? excludedListingId = null)
        {
            var space = await _unitOfWork.spaceRepository.GetAsync(
                x => x.Id == listing.SpaceId && !x.IsDeleted && x.IsActive,
                include: q => q.Include(s => s.ChildSpaces).Include(s => s.ParentSpace));
            if (space == null)
            {
                return "Không tìm thấy mặt bằng với Id đã cho.";
            }

            if (listing.AllowedStartTime == null || listing.AllowedEndTime == null)
            {
                return "Vui lòng thiết lập thời gian bắt đầu và kết thúc cho listing.";
            }

            var allowedStartTime = listing.AllowedStartTime.Value;
            var allowedEndTime = listing.AllowedEndTime.Value;

            var permissionValidation = await ValidateListingCreatorPermissionAsync(space, listing, allowedStartTime, allowedEndTime);
            if (permissionValidation != null)
            {
                return permissionValidation;
            }
           
            var isShareListingByRenter = space.OwnerId != _currentUserService.UserId && listing is SharedListingRequest;
            DateOnly currentTime = DateOnly.FromDateTime(DateTime.Now);
            if (listing.AllowedStartTime != null)
            {
                if (!isShareListingByRenter && listing.AllowedStartTime < currentTime)
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

            var priceUnitValidation = ValidatePriceUnitWithinListingPeriod(listing.PriceUnit, allowedStartTime, allowedEndTime);
            if (priceUnitValidation != null)
            {
                return priceUnitValidation;
            }

            var timeConflictValidation = await ValidateSpaceListingTimeConflictAsync(
                space,
                allowedStartTime,
                allowedEndTime,
                excludedListingId,
                ignoreOwnerSourceListing: isShareListingByRenter,
                sharedListingRequest: listing as SharedListingRequest);
            if (timeConflictValidation != null)
            {
                return timeConflictValidation;
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
                    var availabilityValidation = ValidateShareAvailabilityTime(time, listing.AllowedStartTime, listing.AllowedEndTime);
                    if (availabilityValidation != null)
                    {
                        return availabilityValidation;
                    }
                }

            }

            return null;
        }

        private static string? ValidatePriceUnitWithinListingPeriod(PriceUnit priceUnit, DateOnly allowedStartTime, DateOnly allowedEndTime)
        {
            if (!Enum.IsDefined(typeof(PriceUnit), priceUnit))
            {
                return "Price unit is invalid.";
            }

            var maxPriceUnit = GetMaxPriceUnitForListingPeriod(allowedStartTime, allowedEndTime);
            if (priceUnit > maxPriceUnit)
            {
                return $"Price unit cannot exceed {maxPriceUnit} for the listing rental period.";
            }

            return null;
        }

        private static PriceUnit GetMaxPriceUnitForListingPeriod(DateOnly allowedStartTime, DateOnly allowedEndTime)
        {
            var totalDays = allowedEndTime.DayNumber - allowedStartTime.DayNumber;
            if (totalDays >= 365)
            {
                return PriceUnit.PerYear;
            }

            if (totalDays >= 30)
            {
                return PriceUnit.PerMonth;
            }

            if (totalDays >= 7)
            {
                return PriceUnit.PerWeek;
            }

            return PriceUnit.PerDay;
        }

        private async Task<string?> ValidateListingCreatorPermissionAsync(
            Space space,
            ListingRequest listing,
            DateOnly allowedStartTime,
            DateOnly allowedEndTime)
        {
            var currentUserId = _currentUserService.UserId;
            if (space.OwnerId == currentUserId)
            {
                return null;
            }

            if (listing is not SharedListingRequest)
            {
                return "Bạn không có quyền đăng listing cho mặt bằng này.";
            }

            var usageRights = await _unitOfWork.spaceUsageRightRepository.GetAllAsync(x =>
                x.SpaceId == space.Id &&
                x.UserId == currentUserId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.CanShare &&
                x.Type != SpaceUsageRightType.SubRenter);

            usageRights = usageRights
                .Where(x => DateOnly.FromDateTime(x.ValidFrom) <= allowedStartTime &&
                            DateOnly.FromDateTime(x.ValidTo) >= allowedEndTime)
                .ToList();

            if (!usageRights.Any())
            {
                return "Bạn không có quyền share mặt bằng này trong khoảng thời gian được chọn.";
            }

            return null;
        }

        public async Task<ServiceResult<ShareListingTimePolicyResponse>> GetShareListingTimePolicyAsync(long spaceId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<ShareListingTimePolicyResponse>
                    {
                        IsSuccess = false,
                        Message = "Register first!"
                    };
                }

                var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == spaceId && !x.IsDeleted && x.IsActive);
                if (space == null)
                {
                    return new ServiceResult<ShareListingTimePolicyResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy mặt bằng với Id đã cho."
                    };
                }

                if (space.OwnerId == currentUserId)
                {
                    return new ServiceResult<ShareListingTimePolicyResponse>
                    {
                        IsSuccess = true,
                        Data = new ShareListingTimePolicyResponse
                        {
                            IsLocked = false,
                            Message = "Chủ sở hữu tự nhập thời gian cho share listing."
                        }
                    };
                }

                var contracts = await _unitOfWork.contractRepository.GetAllAsync(x =>
                    x.SpaceId == spaceId &&
                    x.LesseeId == currentUserId &&
                    x.CanShare &&
                    !x.IsDeleted &&
                    x.IsActive &&
                    x.Status == ContractStatusEnum.Active);

                var platformContract = contracts
                    .Where(x => x.Source == ContractSource.Platform)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                if (platformContract != null)
                {
                    return new ServiceResult<ShareListingTimePolicyResponse>
                    {
                        IsSuccess = true,
                        Data = new ShareListingTimePolicyResponse
                        {
                            IsLocked = true,
                            ContractId = platformContract.Id,
                            Source = platformContract.Source,
                            AllowedStartTime = DateOnly.FromDateTime(platformContract.StartDate),
                            AllowedEndTime = DateOnly.FromDateTime(platformContract.EndDate),
                            Message = "Thời gian share được lấy từ hợp đồng trong hệ thống."
                        }
                    };
                }

                var externalContract = contracts
                    .Where(x => x.Source == ContractSource.External)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                return new ServiceResult<ShareListingTimePolicyResponse>
                {
                    IsSuccess = true,
                    Data = new ShareListingTimePolicyResponse
                    {
                        IsLocked = false,
                        ContractId = externalContract?.Id,
                        Source = externalContract?.Source,
                        Message = externalContract != null
                            ? "Vui lòng nhập thời gian share và xác nhận cam kết."
                            : "Không tìm thấy hợp đồng có quyền share cho mặt bằng này."
                    }
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ShareListingTimePolicyResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        private async Task<string?> ValidateSpaceListingTimeConflictAsync(
            Space space,
            DateOnly allowedStartTime,
            DateOnly allowedEndTime,
            long? excludedListingId = null,
            bool ignoreOwnerSourceListing = false,
            SharedListingRequest? sharedListingRequest = null)
        {
            var sameSpaceConflict = await _unitOfWork.listingRepository.GetAllAsync(x =>
                x.SpaceId == space.Id &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == ListingStatusEnum.Accepted &&
                (excludedListingId == null || x.Id != excludedListingId.Value) &&
                x.AllowedStartTime <= allowedEndTime &&
                x.AllowedEndTime >= allowedStartTime);

            if (ignoreOwnerSourceListing)
            {
                sameSpaceConflict = sameSpaceConflict
                    .Where(x => x.CreatorId != space.OwnerId)
                    .ToList();
            }

            if (sharedListingRequest != null)
            {
                var shareConflictMessage = await GetShareListingTimeConflictMessageAsync(
                    sameSpaceConflict,
                    sharedListingRequest.ShareSpaceDetailAvailabilitiesTimes ?? new List<AvailabilitiesTimeRequest>());
                if (shareConflictMessage != null)
                {
                    return shareConflictMessage;
                }

                sameSpaceConflict = sameSpaceConflict
                    .Where(x => x.ListingType != ListingType.SharedSpace)
                    .ToList();
            }

            if (sameSpaceConflict.Any())
            {
                return "Mặt bằng này đã có listing trong khoảng thời gian này.";
            }

            if (space.ParentSpaceId != null)
            {
                var parentConflict = await _unitOfWork.listingRepository.GetAllAsync(x =>
                    x.SpaceId == space.ParentSpaceId.Value &&
                    !x.IsDeleted &&
                    x.IsActive &&
                    x.Status == ListingStatusEnum.Accepted &&
                    (excludedListingId == null || x.Id != excludedListingId.Value) &&
                    x.AllowedStartTime <= allowedEndTime &&
                    x.AllowedEndTime >= allowedStartTime);

                if (parentConflict.Any())
                {
                    return "Mặt bằng lớn đang có listing trong khoảng thời gian này. Chỉ có thể đăng mặt bằng con ngoài khoảng thời gian đó.";
                }
            }
            else
            {
                var childSpaceIds = (space.ChildSpaces ?? Enumerable.Empty<Space>())
                    .Where(x => !x.IsDeleted && x.IsActive)
                    .Select(x => x.Id)
                    .ToList();

                if (childSpaceIds.Any())
                {
                    var childConflict = await _unitOfWork.listingRepository.GetAllAsync(x =>
                        childSpaceIds.Contains(x.SpaceId) &&
                        !x.IsDeleted &&
                        x.IsActive &&
                        x.Status == ListingStatusEnum.Accepted &&
                        (excludedListingId == null || x.Id != excludedListingId.Value) &&
                        x.AllowedStartTime <= allowedEndTime &&
                        x.AllowedEndTime >= allowedStartTime);

                    if (childConflict.Any())
                    {
                        return "Một hoặc nhiều mặt bằng con đang có listing trong khoảng thời gian này. Không thể đăng thuê nguyên mặt bằng lớn trùng thời gian.";
                    }
                }
            }

            return null;
        }

        private static string? ValidateShareAvailabilityTime(
            AvailabilitiesTimeRequest time,
            DateOnly? allowedStartTime,
            DateOnly? allowedEndTime)
        {
            if (time.StartTime == null || time.EndTime == null)
            {
                return "Vui lòng thiết lập thời gian bắt đầu và kết thúc cho từng khung giờ share.";
            }

            if (time.StartTime >= time.EndTime)
            {
                return "Trong khung giờ cho thuê, giờ bắt đầu phải diễn ra trước giờ kết thúc.";
            }

            var hasSpecificDate = time.Specificdate != null;
            var hasDaysOfWeek = time.DaysOfWeek != null && time.DaysOfWeek.Any();
            var hasValidRange = time.ValidFrom != null || time.ValidTo != null;

            if (!hasSpecificDate && !hasDaysOfWeek)
            {
                return "Vui lòng chọn ngày cụ thể hoặc chọn thứ trong tuần cho từng khung giờ share.";
            }

            if (hasSpecificDate && (hasDaysOfWeek || hasValidRange))
            {
                return "Mỗi khung giờ share chỉ được chọn một trong hai loại: ngày cụ thể hoặc lịch lặp theo thứ.";
            }

            if (hasDaysOfWeek && (time.ValidFrom == null || time.ValidTo == null))
            {
                return "Khi chọn lịch lặp theo thứ, vui lòng thiết lập ValidFrom và ValidTo.";
            }

            if (time.ValidFrom != null && time.ValidTo != null && time.ValidFrom > time.ValidTo)
            {
                return "Trong khung giờ cho thuê, ngày bắt đầu (ValidFrom) không được lớn hơn ngày kết thúc (ValidTo).";
            }

            var dateFrom = time.Specificdate ?? time.ValidFrom;
            var dateTo = time.Specificdate ?? time.ValidTo;

            if (allowedStartTime != null && dateFrom != null && dateFrom < allowedStartTime)
            {
                return $"Ngày bắt đầu khung giờ ({dateFrom}) không được sớm hơn thời gian bắt đầu hợp đồng chính ({allowedStartTime}).";
            }

            if (allowedEndTime != null && dateTo != null && dateTo > allowedEndTime)
            {
                return $"Ngày kết thúc khung giờ ({dateTo}) không được trễ hơn thời gian cho phép của hợp đồng ({allowedEndTime}).";
            }

            return null;
        }

        private async Task<string?> GetShareListingTimeConflictMessageAsync(
            IEnumerable<Listing> candidateListings,
            List<AvailabilitiesTimeRequest> requestedTimes)
        {
            var shareListingIds = candidateListings
                .Where(x => x.ListingType == ListingType.SharedSpace)
                .Select(x => x.Id)
                .ToList();

            if (!shareListingIds.Any())
            {
                return null;
            }

            var shareListings = await _unitOfWork.listingRepository.GetAllAsync(
                x => shareListingIds.Contains(x.Id),
                include: q => q.Include(x => x.ShareSpaceDetail)
                               .ThenInclude(x => x.AvailabilitiesTimes));

            foreach (var existing in shareListings)
            {
                if (existing.ShareSpaceDetail?.AvailabilitiesTimes == null)
                {
                    continue;
                }

                foreach (var existingTime in existing.ShareSpaceDetail.AvailabilitiesTimes)
                {
                    foreach (var requestedTime in requestedTimes)
                    {
                        if (!IsAvailabilityOverlapped(requestedTime, existingTime))
                        {
                            continue;
                        }

                        return BuildShareAvailabilityConflictMessage(existing.Id, requestedTime, existingTime);
                    }
                }
            }

            return null;
        }

        private async Task<List<Listing>> FilterShareListingTimeConflictsAsync(
            IEnumerable<Listing> candidateListings,
            List<AvailabilitiesTimeRequest> requestedTimes)
        {
            var candidates = candidateListings.ToList();
            if (!candidates.Any())
            {
                return new List<Listing>();
            }

            var conflicts = candidates
                .Where(x => x.ListingType != ListingType.SharedSpace)
                .ToList();

            var shareListingIds = candidates
                .Where(x => x.ListingType == ListingType.SharedSpace)
                .Select(x => x.Id)
                .ToList();

            if (!shareListingIds.Any())
            {
                return conflicts;
            }

            var shareListings = await _unitOfWork.listingRepository.GetAllAsync(
                x => shareListingIds.Contains(x.Id),
                include: q => q.Include(x => x.ShareSpaceDetail)
                               .ThenInclude(x => x.AvailabilitiesTimes));

            conflicts.AddRange(shareListings.Where(existing =>
                existing.ShareSpaceDetail?.AvailabilitiesTimes != null &&
                existing.ShareSpaceDetail.AvailabilitiesTimes.Any(existingTime =>
                    requestedTimes.Any(requestedTime => IsAvailabilityOverlapped(requestedTime, existingTime)))));

            return conflicts;
        }

        private static bool IsAvailabilityOverlapped(AvailabilitiesTimeRequest requestTime, AvailabilitiesTime existingTime)
        {
            if (requestTime.StartTime == null || requestTime.EndTime == null)
            {
                return false;
            }

            var requestDateFrom = requestTime.ValidFrom ?? requestTime.Specificdate;
            var requestDateTo = requestTime.ValidTo ?? requestTime.Specificdate;
            var existingDateFrom = existingTime.ValidFrom ?? existingTime.Specificdate;
            var existingDateTo = existingTime.ValidTo ?? existingTime.Specificdate;
            if (requestDateFrom == null || requestDateTo == null || existingDateFrom == null || existingDateTo == null)
            {
                return false;
            }

            return IsDatePatternOverlapped(
                       requestTime.Specificdate,
                       requestTime.DaysOfWeek,
                       requestDateFrom.Value,
                       requestDateTo.Value,
                       existingTime.Specificdate,
                       existingTime.DaysOfWeek,
                       existingDateFrom.Value,
                       existingDateTo.Value) &&
                   IsTimeOverlapped(requestTime.StartTime.Value, requestTime.EndTime.Value, existingTime.StartTime, existingTime.EndTime);
        }

        private static string BuildShareAvailabilityConflictMessage(long existingListingId, AvailabilitiesTimeRequest requestTime, AvailabilitiesTime existingTime)
        {
            var requestDateFrom = requestTime.ValidFrom ?? requestTime.Specificdate!.Value;
            var requestDateTo = requestTime.ValidTo ?? requestTime.Specificdate!.Value;
            var existingDateFrom = existingTime.ValidFrom ?? existingTime.Specificdate!.Value;
            var existingDateTo = existingTime.ValidTo ?? existingTime.Specificdate!.Value;

            var dateDescription = DescribeDateOverlap(
                requestTime.Specificdate,
                requestTime.DaysOfWeek,
                requestDateFrom,
                requestDateTo,
                existingTime.Specificdate,
                existingTime.DaysOfWeek,
                existingDateFrom,
                existingDateTo);

            var overlapStart = requestTime.StartTime!.Value > existingTime.StartTime
                ? requestTime.StartTime.Value
                : existingTime.StartTime;
            var overlapEnd = requestTime.EndTime!.Value < existingTime.EndTime
                ? requestTime.EndTime.Value
                : existingTime.EndTime;

            return $"Khung giờ share bị trùng với listing #{existingListingId} trong {dateDescription}, khoảng {overlapStart:HH\\:mm} - {overlapEnd:HH\\:mm}. Vui lòng chọn ngày hoặc giờ khác.";
        }

        private static string DescribeDateOverlap(
            DateOnly? leftSpecificDate,
            List<DayOfWeek>? leftDaysOfWeek,
            DateOnly leftValidFrom,
            DateOnly leftValidTo,
            DateOnly? rightSpecificDate,
            List<DayOfWeek>? rightDaysOfWeek,
            DateOnly rightValidFrom,
            DateOnly rightValidTo)
        {
            if (leftSpecificDate != null && rightSpecificDate != null)
            {
                return $"ngày {leftSpecificDate:yyyy-MM-dd}";
            }

            if (leftSpecificDate != null)
            {
                return $"ngày {leftSpecificDate:yyyy-MM-dd}";
            }

            if (rightSpecificDate != null)
            {
                return $"ngày {rightSpecificDate:yyyy-MM-dd}";
            }

            var overlapFrom = leftValidFrom > rightValidFrom ? leftValidFrom : rightValidFrom;
            var overlapTo = leftValidTo < rightValidTo ? leftValidTo : rightValidTo;
            var overlapDays = (leftDaysOfWeek ?? new List<DayOfWeek>())
                .Intersect(rightDaysOfWeek ?? new List<DayOfWeek>())
                .ToList();

            return $"các ngày {string.Join(", ", overlapDays)} từ {overlapFrom:yyyy-MM-dd} đến {overlapTo:yyyy-MM-dd}";
        }

        private static bool IsDatePatternOverlapped(
            DateOnly? leftSpecificDate,
            List<DayOfWeek>? leftDaysOfWeek,
            DateOnly leftValidFrom,
            DateOnly leftValidTo,
            DateOnly? rightSpecificDate,
            List<DayOfWeek>? rightDaysOfWeek,
            DateOnly rightValidFrom,
            DateOnly rightValidTo)
        {
            if (leftValidFrom > rightValidTo || rightValidFrom > leftValidTo)
            {
                return false;
            }

            if (leftSpecificDate != null && rightSpecificDate != null)
            {
                return leftSpecificDate == rightSpecificDate;
            }

            if (leftSpecificDate != null)
            {
                return IsSpecificDateMatched(leftSpecificDate.Value, rightValidFrom, rightValidTo, rightDaysOfWeek);
            }

            if (rightSpecificDate != null)
            {
                return IsSpecificDateMatched(rightSpecificDate.Value, leftValidFrom, leftValidTo, leftDaysOfWeek);
            }

            return (leftDaysOfWeek ?? new List<DayOfWeek>())
                .Intersect(rightDaysOfWeek ?? new List<DayOfWeek>())
                .Any();
        }

        private static bool IsSpecificDateMatched(DateOnly specificDate, DateOnly validFrom, DateOnly validTo, List<DayOfWeek>? daysOfWeek)
        {
            return specificDate >= validFrom &&
                   specificDate <= validTo &&
                   (daysOfWeek ?? new List<DayOfWeek>()).Contains(specificDate.DayOfWeek);
        }

        private static bool IsTimeOverlapped(TimeOnly leftStart, TimeOnly leftEnd, TimeOnly rightStart, TimeOnly rightEnd)
        {
            return leftStart < rightEnd && leftEnd > rightStart;
        }

        public async Task<ServiceResult<ListingResponse>> CreateListingAsync(ListingRequest listing, decimal amount, int durationInDays)
        {
            try
            {
                var wallet = await _walletService.SpendWalletBalance(amount, "Thanh toán bài đăng");
                if(wallet.IsSuccess == false)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = wallet.Message
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
                var newListing = _mapper.Map<Listing>(listing);
                newListing.CreatorId = _currentUserService.UserId;
                newListing.CreatedAt = DateTime.Now;
                newListing.IsActive = true;
                newListing.Status = Domain.Enum.ListingStatusEnum.Accepted;
                newListing.ListingType = ListingType.EntireSpace;
                newListing.priorityLevel = amount;
                newListing.durationInDays = durationInDays;
                await _unitOfWork.listingRepository.AddAsync(newListing);
                await _unitOfWork.SaveChangesAsync();
                var listingResult = await _unitOfWork.listingRepository.GetAsync(x => x.Id == newListing.Id, include: q => q.Include(l => l.Space).Include(l => l.Lessor));
                var result = _mapper.Map<ListingResponse>(listingResult);

                _ = Task.Run(async () => await InvalidateListingCacheAsync(newListing.Id));

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
                string cacheKey = $"Cache:Listing:Id_{id}";
                var cachedListing = await GetCacheAsync<ListingResponse>(cacheKey);
                if (cachedListing != null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = true,
                        Data = cachedListing
                    };
                }

                var listing = await _unitOfWork.listingRepository.GetAsync(
                    x => x.Id == id,
                    include: q => q.Include(l => l.Space)
                                   .Include(l => l.Lessor)
                                   .Include(l => l.PictureURLs));
                if (listing == null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy listing với Id đã cho."
                    };
                }
                var result = _mapper.Map<ListingResponse>(listing);
                await SetCacheAsync(cacheKey, result);
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
                var checkValidation = await ValidationMessageAsync(listing, id);
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

                _ = Task.Run(async () => await InvalidateListingCacheAsync(id));

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
                _ = Task.Run(async () => await InvalidateListingCacheAsync(id));
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

        public async Task<ServiceResult<List<ShareListingResponse>>> GetAllListingsAsync(ListingStatusEnum? status, ListingType? listingType = null, int? top = null)
        {
            try
            {
                string cacheKey = GetListingCacheKey(status, listingType);
                var cachedListings = await GetCacheAsync<List<ShareListingResponse>>(cacheKey);
                if (cachedListings != null)
                {
                    return new ServiceResult<List<ShareListingResponse>>
                    {
                        IsSuccess = true,
                        Data = cachedListings
                    };
                }
                var listings = await _unitOfWork.listingRepository.GetAllWithSortAsync(
                    l => !l.IsDeleted
                        && (status == null || l.Status == status)
                        && (listingType == null || l.ListingType == listingType),
                    orderBy: l => l.OrderByDescending(x => x.priorityLevel).ThenByDescending(x => x.CreatedAt),
                    include: q => q.Include(l => l.Space)
                                    .Include(l => l.Lessor)
                                    .Include(l => l.ShareSpaceDetail)
                                        .ThenInclude(s => s.AvailabilitiesTimes)
                                    .Include(l => l.ShareSpaceDetail)
                                        .ThenInclude(s => s.ShareSpaceAmenities)
                                    .Include(l => l.ShareSpaceDetail)
                                        .ThenInclude(s => s.ShareSpaceCategories)
                                    .Include(l => l.PictureURLs),
                    take: top);

                var mappedListings = _mapper.Map<List<ShareListingResponse>>(listings);
                await SetCacheAsync(cacheKey, mappedListings);
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
                else if (request.Status == Domain.Enum.ListingStatusEnum.Ban)
                {
                    if (string.IsNullOrWhiteSpace(request.CancelReason))
                    {
                        return new ServiceResult<ListingResponse>
                        {
                            IsSuccess = false,
                            Message = "Vui lòng cung cấp lý do hủy."
                        };
                    }
                    listing.Status = Domain.Enum.ListingStatusEnum.Ban;
                    listing.IsActive = false;
                    listing.CancelReason = request.CancelReason;

                    var reports = await _unitOfWork.listingReportRepository.GetAllAsync(x => x.ListingId == listing.Id);
                    foreach (var report in reports)
                    {
                        report.IsBanned = true;
                        await _unitOfWork.listingReportRepository.UpdateAsync(report);
                    }
                }
                else
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Trạng thái không hợp lệ."
                    };
                }
                await _unitOfWork.listingRepository.UpdateAsync(listing);
                await _unitOfWork.SaveChangesAsync();
                _ = Task.Run(async () => await InvalidateListingCacheAsync(id));
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

        public async Task<ServiceResult<ListingReportResponse>> CreateListingReportAsync(CreateListingReportRequest request)
        {
            try
            {
                if (request.ListingId <= 0)
                {
                    return new ServiceResult<ListingReportResponse>
                    {
                        IsSuccess = false,
                        Message = "Id bài đăng không hợp lệ."
                    };
                }

                if (request.Reasons == null || !request.Reasons.Any())
                {
                    return new ServiceResult<ListingReportResponse>
                    {
                        IsSuccess = false,
                        Message = "Vui lòng chọn ít nhất một lý do báo cáo."
                    };
                }

                var reporterId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(reporterId))
                {
                    return new ServiceResult<ListingReportResponse>
                    {
                        IsSuccess = false,
                        Message = "Bạn cần đăng nhập để gửi báo cáo."
                    };
                }

                var listing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == request.ListingId && !x.IsDeleted);
                if (listing == null)
                {
                    return new ServiceResult<ListingReportResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy bài đăng để báo cáo."
                    };
                }

                var existingReport = await _unitOfWork.listingReportRepository.GetAsync(x => x.ListingId == request.ListingId && x.ReporterId == reporterId);
                if (existingReport != null)
                {
                    return new ServiceResult<ListingReportResponse>
                    {
                        IsSuccess = false,
                        Message = "Bạn đã gửi báo cáo cho bài đăng này rồi."
                    };
                }

                var newReport = new ListingReport
                {
                    ListingId = request.ListingId,
                    ReporterId = reporterId,
                    ReasonType = string.Join(",", request.Reasons.Select(x => x.ToString())),
                    AdditionalDetails = request.AdditionalDetails ?? string.Empty,
                    IsBanned = false
                };

                await _unitOfWork.listingReportRepository.AddAsync(newReport);
                await _unitOfWork.SaveChangesAsync();
                _ = Task.Run(async () => await InvalidateListingCacheAsync(request.ListingId));

                return new ServiceResult<ListingReportResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ListingReportResponse>(newReport),
                    Message = "Gửi báo cáo thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ListingReportResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<List<ListingReportResponse>>> GetListingReportsAsync(long listingId)
        {
            try
            {
                string cacheKey = $"Cache:ListingReports:Listing_{listingId}";
                var cachedReports = await GetCacheAsync<List<ListingReportResponse>>(cacheKey);
                if (cachedReports != null)
                {
                    return new ServiceResult<List<ListingReportResponse>>
                    {
                        IsSuccess = true,
                        Data = cachedReports
                    };
                }

                var listing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == listingId && !x.IsDeleted);
                if (listing == null)
                {
                    return new ServiceResult<List<ListingReportResponse>>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy bài đăng."
                    };
                }

                var reports = await _unitOfWork.listingReportRepository.GetAllAsync(
                    x => x.ListingId == listingId,
                    include: q => q.Include(x => x.Reporter));

                reports = reports.OrderByDescending(x => x.CreatedAt).ToList();
                var mappedReports = _mapper.Map<List<ListingReportResponse>>(reports);
                await SetCacheAsync(cacheKey, mappedReports);

                return new ServiceResult<List<ListingReportResponse>>
                {
                    IsSuccess = true,
                    Data = mappedReports
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<ListingReportResponse>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<List<ReportedListingSummaryResponse>>> GetReportedListingsAsync()
        {
            try
            {
                const string cacheKey = "Cache:ListingReports:ReportedListings";
                var cachedSummaries = await GetCacheAsync<List<ReportedListingSummaryResponse>>(cacheKey);
                if (cachedSummaries != null)
                {
                    return new ServiceResult<List<ReportedListingSummaryResponse>>
                    {
                        IsSuccess = true,
                        Data = cachedSummaries
                    };
                }

                var reports = await _unitOfWork.listingReportRepository.GetAllWithSortAsync(
                    filter: null,
                    include: q => q.Include(x => x.Listing));

                var summaries = reports
                    .GroupBy(x => x.ListingId)
                    .Select(g => new ReportedListingSummaryResponse
                    {
                        ListingId = g.Key,
                        ReportCount = g.Count(),
                        IsBanned = g.Any(x => x.IsBanned),
                        ListingStatus = g.First().Listing.Status.ToString(),
                        ListingDescription = g.First().Listing.Description
                    })
                    .OrderByDescending(x => x.ReportCount)
                    .ToList();
                await SetCacheAsync(cacheKey, summaries);

                return new ServiceResult<List<ReportedListingSummaryResponse>>
                {
                    IsSuccess = true,
                    Data = summaries
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<ReportedListingSummaryResponse>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ListingReportDetailResponse>> GetListingReportDetailAsync(long listingId)
        {
            try
            {
                string cacheKey = $"Cache:ListingReports:Detail_{listingId}";
                var cachedDetail = await GetCacheAsync<ListingReportDetailResponse>(cacheKey);
                if (cachedDetail != null)
                {
                    return new ServiceResult<ListingReportDetailResponse>
                    {
                        IsSuccess = true,
                        Data = cachedDetail
                    };
                }

                var reports = await _unitOfWork.listingReportRepository.GetAllAsync(x => x.ListingId == listingId);

                var reasonBreakdown = reports
                    .SelectMany(x => ParseReasons(x.ReasonType))
                    .GroupBy(x => x.ToString())
                    .Select(g => new ReportReasonCountResponse
                    {
                        Reason = g.Key ?? "Other",
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();
                var detail = new ListingReportDetailResponse
                {
                    ListingId = listingId,
                    TotalReportCount = reports.Count,
                    ReasonBreakdown = reasonBreakdown
                };
                await SetCacheAsync(cacheKey, detail);

                return new ServiceResult<ListingReportDetailResponse>
                {
                    IsSuccess = true,
                    Data = detail
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ListingReportDetailResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<int>> ClearListingReportsAsync(long listingId)
        {
            try
            {
                if (listingId <= 0)
                {
                    return new ServiceResult<int>
                    {
                        IsSuccess = false,
                        Message = "Id bài đăng không hợp lệ."
                    };
                }

                var listing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == listingId && !x.IsDeleted);
                if (listing == null)
                {
                    return new ServiceResult<int>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy bài đăng."
                    };
                }

                var reports = await _unitOfWork.listingReportRepository.GetAllAsync(x => x.ListingId == listingId);
                if (!reports.Any())
                {
                    return new ServiceResult<int>
                    {
                        IsSuccess = true,
                        Data = 0,
                        Message = "Bài đăng này không có report cần gỡ."
                    };
                }

                var removedCount = reports.Count;
                await _unitOfWork.listingReportRepository.RemoveRangeAsync(reports);
                await _unitOfWork.SaveChangesAsync();
                _ = Task.Run(async () => await InvalidateListingCacheAsync(listingId));

                return new ServiceResult<int>
                {
                    IsSuccess = true,
                    Data = removedCount,
                    Message = "Đã gỡ bài đăng khỏi danh sách report."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<int>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        private static List<ReportReasonEnum> ParseReasons(string reasons)
        {
            if (string.IsNullOrWhiteSpace(reasons))
            {
                return new List<ReportReasonEnum>();
            }

            return reasons
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => Enum.TryParse<ReportReasonEnum>(x, out var reason) ? reason : ReportReasonEnum.Other)
                .ToList();
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
                listing.IsActive = false;
                listing.UpdatedAt = DateTime.Now;
                await _unitOfWork.listingRepository.UpdateAsync(listing);

                var banner = await _unitOfWork.bannerRepository.GetAsync(x => x.ListingId == id && !x.IsDeleted);
                if (banner != null)
                {
                    banner.IsDeleted = true;
                    banner.IsActive = false;
                    banner.UpdatedAt = DateTime.Now;
                    banner.UpdatedBy = _currentUserService.UserId ?? "System";
                    await _unitOfWork.bannerRepository.UpdateAsync(banner);
                }

                await _unitOfWork.SaveChangesAsync();
                _ = Task.Run(async () => await InvalidateListingCacheAsync(id));
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

        public async Task<ServiceResult<List<ShareListingResponse>>> GetSoftDeletedListingsAsync(ListingType? listingType = null)
        {
            try
            {
                var listings = await _unitOfWork.listingRepository.GetAllWithSortAsync(
                    l => l.IsDeleted && (listingType == null || l.ListingType == listingType),
                    orderBy: l => l.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.CreatedAt),
                    include: q => q.Include(l => l.Space)
                                    .Include(l => l.Lessor)
                                    .Include(l => l.ShareSpaceDetail)
                                        .ThenInclude(s => s.AvailabilitiesTimes)
                                    .Include(l => l.ShareSpaceDetail)
                                        .ThenInclude(s => s.ShareSpaceAmenities)
                                    .Include(l => l.ShareSpaceDetail)
                                        .ThenInclude(s => s.ShareSpaceCategories)
                                    .Include(l => l.PictureURLs));

                return new ServiceResult<List<ShareListingResponse>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<ShareListingResponse>>(listings)
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

        public async Task<ServiceResult<ListingResponse>> RestoreListingAsync(long id)
        {
            try
            {
                var listing = await _unitOfWork.listingRepository.GetAsync(
                    x => x.Id == id && x.IsDeleted,
                    include: q => q.Include(l => l.Space).Include(l => l.Lessor));

                if (listing == null)
                {
                    return new ServiceResult<ListingResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy listing đã xóa mềm với Id đã cho."
                    };
                }

                listing.IsDeleted = false;
                listing.IsActive = true;
                listing.UpdatedAt = DateTime.Now;

                await _unitOfWork.listingRepository.UpdateAsync(listing);
                await _unitOfWork.SaveChangesAsync();
                _ = Task.Run(async () => await InvalidateListingCacheAsync(id));

                return new ServiceResult<ListingResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ListingResponse>(listing),
                    Message = "Khôi phục listing thành công."
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

        public async Task<ServiceResult<ShareListingResponse>> CreateShareListingAsync(SharedListingRequest sharedListingRequest, decimal amount, int durationInDays)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletService.SpendWalletBalance(amount, "Thanh toán bài đăng");
                if (wallet.IsSuccess == false)
                {
                    return new ServiceResult<ShareListingResponse>
                    {
                        IsSuccess = false,
                        Message = wallet.Message
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
                newListing.IsActive = true;
                newListing.Status = Domain.Enum.ListingStatusEnum.Accepted;
                newListing.ListingType = ListingType.SharedSpace;
                newListing.priorityLevel = amount;
                newListing.durationInDays = durationInDays;
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
                                    .ThenInclude(l => l.ShareSpaceCategories)
                                    .Include(l => l.PictureURLs));

                var result = _mapper.Map<ShareListingResponse>(listingResult);
                _ = Task.Run(async () => await InvalidateListingCacheAsync(newListing.Id));

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
                                    .ThenInclude(l => l.ShareSpaceCategories)
                                    .Include(l => l.PictureURLs));

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

                var checkValidation = await ValidationMessageAsync(sharedListingRequest, id);
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
                                    .ThenInclude(l => l.ShareSpaceCategories)
                                    .Include(l => l.PictureURLs));

                var result = _mapper.Map<ShareListingResponse>(listingResult);
                _ = Task.Run(async () => await InvalidateListingCacheAsync(id));

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

        public async Task<ServiceResult<int>> DeactivateExpiredListingsAsync()
        {
            try
            {
                var currentDate = DateOnly.FromDateTime(DateTime.Now);
                var expiredListings = await _unitOfWork.listingRepository.GetAllAsync(x =>
                    !x.IsDeleted &&
                    x.IsActive &&
                    x.Status == ListingStatusEnum.Accepted);

                if (!expiredListings.Any())
                {
                    return new ServiceResult<int>
                    {
                        IsSuccess = true,
                        Data = 0,
                        Message = "Không có listing nào hết hạn cần vô hiệu hóa."
                    };
                }

                int count = 0;
                foreach (var listing in expiredListings)
                {
                    if (listing.CreatedAt + TimeSpan.FromDays(listing.durationInDays) < DateTime.Now)
                    {
                        listing.IsDeleted = true;
                        listing.IsActive = false;
                        listing.UpdatedAt = DateTime.Now;
                        listing.UpdatedBy = "SystemBackgroundWorker";
                        await _unitOfWork.listingRepository.UpdateAsync(listing);
                        if(listing.Banner != null)
                        {
                            listing.Banner.IsDeleted = true;
                            await _unitOfWork.bannerRepository.UpdateAsync(listing.Banner);
                        }
                        count++;
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                _ = Task.Run(async () => await InvalidateListingCacheAsync(null));

                return new ServiceResult<int>
                {
                    IsSuccess = true,
                    Data = count,
                    Message = $"Đã tự động xóa mềm {count} listing hết hạn."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<int>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
    }
}

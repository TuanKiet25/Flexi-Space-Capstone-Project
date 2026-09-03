using AutoMapper;
using FlexiSpace.Application;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Services
{
    public class BannerService : IBannerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletService _walletService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly string AdminCreated = "Admin";

        public BannerService(
            IUnitOfWork unitOfWork,
            IWalletService walletService,
            IMapper mapper,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _walletService = walletService;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<IEnumerable<BannerResponse>>> GetAllAsync()
        {
            try
            {
                var bannerPriorityLevel = await _unitOfWork.priorityLevelRepository.GetAsync(b => b.Type == Domain.Enum.PriorityLevelTypeEnum.Banner && b.IsActive);
                if (bannerPriorityLevel == null)
                {
                    return new ServiceResult<IEnumerable<BannerResponse>>
                    {
                        IsSuccess = false,
                        Message = "Active banner priority level configuration not found."
                    };
                }

                var adminBanners = await _unitOfWork.bannerRepository.GetAllWithSortAsync(
                    filter: b => !b.IsDeleted && b.CreatedBy == AdminCreated,
                    include: q => q.Include(b => b.PictureURL).Include(b => b.Listing)
                );

                var userBanners = await _unitOfWork.bannerRepository.GetAllWithSortAsync(
                    filter: b => !b.IsDeleted && b.CreatedBy != AdminCreated,
                    include: q => q.Include(b => b.PictureURL).Include(b => b.Listing),
                    take: bannerPriorityLevel.DurationForBanner,
                    orderBy: q => q.OrderByDescending(b => b.CreatedAt)
                );

                var result = _mapper.Map<IEnumerable<BannerResponse>>(adminBanners.Concat(userBanners));
                return new ServiceResult<IEnumerable<BannerResponse>>
                {
                    IsSuccess = true,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<BannerResponse>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<BannerResponse>> GetByIdAsync(long id)
        {
            try
            {
                var banner = await _unitOfWork.bannerRepository.GetAsync(
                    filter: b => b.Id == id && !b.IsDeleted,
                    include: q => q.Include(b => b.PictureURL).Include(b => b.Listing)
                );

                if (banner == null)
                {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = "Banner not found."
                    };
                }

                var result = _mapper.Map<BannerResponse>(banner);
                return new ServiceResult<BannerResponse>
                {
                    IsSuccess = true,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<BannerResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<BannerResponse>> CreateForUserAsync(CreateBannerRequest request, int durationInDays, decimal price)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                if (!request.ListingId.HasValue)
                {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = "ListingId is required for user banner."
                    };
                }

                // Look up listing
                var listing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == request.ListingId.Value && !x.IsDeleted);
                if (listing == null)
                {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = "Listing not found."
                    };
                }

                // Check if user owns the listing
                if (listing.CreatorId != currentUserId)
                {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = "You do not own this listing."
                    };
                }

                // Check if banner already exists
                var existingBanner = await _unitOfWork.bannerRepository.GetAsync(x => x.ListingId == request.ListingId.Value && !x.IsDeleted);
                if (existingBanner != null)
                {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = "This listing already has a banner."
                    };
                }

                // Check if user has reached the maximum number of banners
                var userBanners = await _unitOfWork.bannerRepository.GetAllWithSortAsync(
                    filter: b => !b.IsDeleted && b.CreatedBy != AdminCreated,
                    include: q => q.Include(b => b.PictureURL).Include(b => b.Listing)
                );
                var bannerPriorityLevel = await _unitOfWork.priorityLevelRepository.GetAsync(b => b.Type == Domain.Enum.PriorityLevelTypeEnum.Banner && b.IsActive);
                if (bannerPriorityLevel == null)
                {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = "Active banner priority level configuration not found."
                    };
                }

                if (userBanners.Count() >= bannerPriorityLevel.DurationForBanner) {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = "You have reached the maximum number of banners allowed."
                    };
                }

                // Spend from wallet
                var spendResult = await _walletService.SpendWalletBalance(price, $"Thanh toán tạo Banner quảng cáo cho Listing #{request.ListingId.Value}");
                if (!spendResult.IsSuccess)
                {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = spendResult.Message ?? "Failed to spend wallet balance."
                    };
                }

                // Create Banner
                var newBanner = _mapper.Map<Banner>(request);
                newBanner.DurationInDays = durationInDays;
                newBanner.CreatedAt = DateTime.Now;
                newBanner.CreatedBy = currentUserId;
                newBanner.IsActive = true;
                newBanner.IsDeleted = false;

                await _unitOfWork.bannerRepository.AddAsync(newBanner);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<BannerResponse>(newBanner);
                return new ServiceResult<BannerResponse>
                {
                    IsSuccess = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<BannerResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<BannerResponse>> CreateForAdminAsync(CreateBannerRequest request, int durationInDays)
        {
            try
            {
                var currentUserId = AdminCreated;
                if (request.ListingId.HasValue)
                {
                    // Check if listing exists
                    var listing = await _unitOfWork.listingRepository.GetAsync(x => x.Id == request.ListingId.Value && !x.IsDeleted);
                    if (listing == null)
                    {
                        return new ServiceResult<BannerResponse>
                        {
                            IsSuccess = false,
                            Message = "Listing not found."
                        };
                    }

                    // Check if banner already exists
                    var existingBanner = await _unitOfWork.bannerRepository.GetAsync(x => x.ListingId == request.ListingId.Value && !x.IsDeleted);
                    if (existingBanner != null)
                    {
                        return new ServiceResult<BannerResponse>
                        {
                            IsSuccess = false,
                            Message = "This listing already has a banner."
                        };
                    }
                }

                // Create Banner (without wallet deduction)
                var newBanner = _mapper.Map<Banner>(request);
                newBanner.DurationInDays = durationInDays;
                newBanner.CreatedAt = DateTime.Now;
                newBanner.CreatedBy = currentUserId;
                newBanner.IsActive = true;
                newBanner.IsDeleted = false;

                await _unitOfWork.bannerRepository.AddAsync(newBanner);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<BannerResponse>(newBanner);
                return new ServiceResult<BannerResponse>
                {
                    IsSuccess = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<BannerResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<BannerResponse>> UpdateAsync(long id, UpdateBannerRequest request)
        {
            try
            {
                var banner = await _unitOfWork.bannerRepository.GetAsync(
                    filter: b => b.Id == id && !b.IsDeleted,
                    include: q => q.Include(b => b.PictureURL)
                );

                if (banner == null)
                {
                    return new ServiceResult<BannerResponse>
                    {
                        IsSuccess = false,
                        Message = "Banner not found."
                    };
                }

                // Update properties
                banner.Title = request.Title;
                banner.Description = request.Description;
                banner.IsActive = request.IsActive;
                banner.UpdatedAt = DateTime.Now;
                banner.UpdatedBy = _currentUserService.UserId ?? "System";

                await _unitOfWork.bannerRepository.UpdateAsync(banner);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<BannerResponse>(banner);
                return new ServiceResult<BannerResponse>
                {
                    IsSuccess = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<BannerResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<string>> DeleteAsync(long id)
        {
            try
            {
                var banner = await _unitOfWork.bannerRepository.GetAsync(
                    filter: b => b.Id == id && !b.IsDeleted,
                    include: q => q.Include(b => b.PictureURL)
                );

                if (banner == null)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "Banner not found."
                    };
                }

                banner.IsDeleted = true;
                banner.IsActive = false;
                banner.UpdatedAt = DateTime.Now;
                banner.UpdatedBy = _currentUserService.UserId ?? "System";

                await _unitOfWork.bannerRepository.UpdateAsync(banner);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<string>
                {
                    IsSuccess = true,
                    Data = "Banner deleted successfully."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<string>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<int>> DeleteExpiredBannersAsync()
        {
            try
            {
                var banners = await _unitOfWork.bannerRepository.GetAllAsync(b => !b.IsDeleted && b.IsActive);

                if (!banners.Any())
                {
                    return new ServiceResult<int>
                    {
                        IsSuccess = true,
                        Data = 0,
                        Message = "Không có banner nào hết hạn cần xóa."
                    };
                }

                int count = 0;
                foreach (var banner in banners)
                {
                    if (banner.CreatedAt + TimeSpan.FromDays(banner.DurationInDays) < DateTime.Now)
                    {
                        banner.IsDeleted = true;
                        banner.IsActive = false;
                        banner.UpdatedAt = DateTime.Now;
                        banner.UpdatedBy = "SystemBackgroundWorker";
                        await _unitOfWork.bannerRepository.UpdateAsync(banner);
                        count++;
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<int>
                {
                    IsSuccess = true,
                    Data = count,
                    Message = $"Đã tự động xóa {count} banner hết hạn."
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

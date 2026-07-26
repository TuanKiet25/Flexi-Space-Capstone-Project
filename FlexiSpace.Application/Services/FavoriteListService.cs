using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Application.Services
{
    public class FavoriteListService : IFavoriteListService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public FavoriteListService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;

        }

        public async Task<ServiceResult<FavoriteListResponse>> AddListingsAsync(IEnumerable<long> listingIds)
        {
            var ids = listingIds?.Distinct().ToList() ?? new List<long>();
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId) || ids.Count == 0)
                return Failure<FavoriteListResponse>("UserId và ít nhất một ListingId là bắt buộc.");
            var listings = await _unitOfWork.listingRepository.GetAllAsync(
                x => ids.Contains(x.Id) && !x.IsDeleted);
            var missingIds = ids.Except(listings.Select(x => x.Id)).ToList();
            if (missingIds.Count > 0)
                return NotFound<FavoriteListResponse>(
                    $"Không tìm thấy listing: {string.Join(", ", missingIds)}.");

            var favoriteList = await GetEntityAsync(userId);
            if (favoriteList == null)
            {
                favoriteList = new FavoriteList
                {
                    UserId = userId,
                    Name = "Favorite space list",
                    CreatedBy = userId,
                    IsActive = true
                };
                await _unitOfWork.favoriteListRepository.AddAsync(favoriteList);
            }

            var existingIds = favoriteList.FavoriteListings.Select(x => x.ListingId).ToHashSet();
            foreach (var id in ids.Where(id => !existingIds.Contains(id)))
            {
                favoriteList.FavoriteListings.Add(new FavoriteListing
                {
                    ListingId = id,
                    CreatedAt = DateTime.Now
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return Success(await MapResponseAsync(userId), "Đã thêm listing vào danh sách yêu thích.");
        }

        public async Task<ServiceResult<FavoriteListIdsResponse>> GetByUserIdAsync()
        {
            var userId = _currentUserService.UserId;
            if(string.IsNullOrWhiteSpace(userId))
                return Failure<FavoriteListIdsResponse>("UserId là bắt buộc.");
            var response = await MapResponseIdAsync(userId);
            return response == null
                ? NotFound<FavoriteListIdsResponse>("User chưa có danh sách yêu thích.")
                : Success(response);
        }

        public async Task<ServiceResult<ListingResponse>> GetListingDetailAsync(long listingId)
        {
            var userId = _currentUserService.UserId;
            if(string.IsNullOrWhiteSpace(userId))
                return Failure<ListingResponse>("UserId là bắt buộc.");
            var favoriteList = await GetEntityAsync(userId);
            var favorite = favoriteList?.FavoriteListings.FirstOrDefault(x => x.ListingId == listingId);
            return favorite == null
                ? NotFound<ListingResponse>("Listing không có trong danh sách yêu thích của user.")
                : Success(_mapper.Map<ListingResponse>(favorite.Listing));
        }

        public async Task<ServiceResult<FavoriteListIdsResponse>> RemoveListingAsync(long listingId)
        {
            var userId = _currentUserService.UserId;
            if(string.IsNullOrWhiteSpace(userId))
                return Failure<FavoriteListIdsResponse>("UserId là bắt buộc.");
            var favoriteList = await GetEntityAsync(userId);
            if (favoriteList == null)
                return NotFound<FavoriteListIdsResponse>("User chưa có danh sách yêu thích.");

            var favorite = favoriteList.FavoriteListings.FirstOrDefault(x => x.ListingId == listingId);
            if (favorite == null)
                return NotFound<FavoriteListIdsResponse>("Listing không có trong danh sách yêu thích của user.");

            favoriteList.FavoriteListings.Remove(favorite);
            await _unitOfWork.SaveChangesAsync();
            return Success(await MapResponseIdAsync(userId), "Đã xóa listing khỏi danh sách yêu thích.");
        }
        private async Task<FavoriteList?> GetEntityForUpdateAsync(string userId) =>
            await _unitOfWork.favoriteListRepository.GetAsync(
                x => x.UserId == userId && !x.IsDeleted,
                q => q.Include(x => x.FavoriteListings));
        private async Task<FavoriteList?> GetEntityAsync(string userId) =>
            await _unitOfWork.favoriteListRepository.GetAsync(
                x => x.UserId == userId && !x.IsDeleted,
                q => q.Include(x => x.FavoriteListings)
                    .ThenInclude(x => x.Listing).ThenInclude(x => x.Space)
                    .Include(x => x.FavoriteListings)
                    .ThenInclude(x => x.Listing).ThenInclude(x => x.Lessor)
                    .Include(x => x.FavoriteListings)
                    .ThenInclude(x => x.Listing).ThenInclude(x => x.PictureURLs));

        private async Task<FavoriteListResponse?> MapResponseAsync(string userId)
        {
            var entity = await GetEntityAsync(userId);
            return entity == null ? null : _mapper.Map<FavoriteListResponse>(entity);
        }
        private async Task<FavoriteListIdsResponse?> MapResponseIdAsync(string userId)
        {
            var entity = await GetEntityForUpdateAsync(userId);
            return entity == null ? null : _mapper.Map<FavoriteListIdsResponse>(entity);
        }
        private static ServiceResult<T> Success<T>(T? data, string? message = null) =>
            new() { IsSuccess = true, Data = data, Message = message };

        private static ServiceResult<T> Failure<T>(string message) =>
            new() { IsSuccess = false, Message = message };

        private static ServiceResult<T> NotFound<T>(string message) =>
            new() { IsSuccess = false, IsNotFound = true, Message = message };
    }
}

using FlexiSpace.Application.ViewModels.Responses;

namespace FlexiSpace.Application.IServices
{
    public interface IFavoriteListService
    {
        Task<ServiceResult<FavoriteListResponse>> AddListingsAsync(IEnumerable<long> listingIds);
        Task<ServiceResult<FavoriteListIdsResponse>> GetByUserIdAsync();
        Task<ServiceResult<ListingResponse>> GetListingDetailAsync(long listingId);
        Task<ServiceResult<FavoriteListIdsResponse>> RemoveListingAsync(long listingId);
    }
}

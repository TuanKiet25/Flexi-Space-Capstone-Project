using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IListingService
    {
        Task<ServiceResult<ListingResponse>> CreateListingAsync(ListingRequest listing, decimal amount, int durationInDays);
        Task<ServiceResult<List<ShareListingResponse>>> GetAllListingsAsync(ListingStatusEnum? status, ListingType? listingType = null, int? top = null);
        Task<ServiceResult<ListingResponse>> GetListingByIdAsync(long id);
        Task<ServiceResult<ListingResponse>> UpdateListingAsync(long id, ListingRequest listing);
        Task<ServiceResult<ListingResponse>> HardDeleteListingAsync(long id);
        Task<ServiceResult<ListingResponse>> AcceptOrCancelListingAsync(long id, ListingStatusRequest request);
        Task<ServiceResult<ListingResponse>> SoftDeleteListingAsync(long id);
        Task<ServiceResult<List<ShareListingResponse>>> GetSoftDeletedListingsAsync(ListingType? listingType = null);
        Task<ServiceResult<ListingResponse>> RestoreListingAsync(long id);
        Task<ServiceResult<ListingReportResponse>> CreateListingReportAsync(CreateListingReportRequest request);
        Task<ServiceResult<List<ListingReportResponse>>> GetListingReportsAsync(long listingId);
        Task<ServiceResult<List<ReportedListingSummaryResponse>>> GetReportedListingsAsync();
        Task<ServiceResult<ListingReportDetailResponse>> GetListingReportDetailAsync(long listingId);
        Task<ServiceResult<int>> ClearListingReportsAsync(long listingId);
        Task<ServiceResult<ShareListingTimePolicyResponse>> GetShareListingTimePolicyAsync(long spaceId);
        Task<ServiceResult<ShareListingResponse>> CreateShareListingAsync(SharedListingRequest sharedListingRequest, decimal amount, int durationInDays);
        Task<ServiceResult<ShareListingResponse>> UpdateShareListingAsync(long id, SharedListingRequest sharedListingRequest);
        Task<ServiceResult<int>> DeactivateExpiredListingsAsync();
    }
}

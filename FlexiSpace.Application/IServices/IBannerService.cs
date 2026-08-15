using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IBannerService
    {
        Task<ServiceResult<IEnumerable<BannerResponse>>> GetAllAsync();
        Task<ServiceResult<BannerResponse>> GetByIdAsync(long id);
        Task<ServiceResult<BannerResponse>> CreateForUserAsync(CreateBannerRequest request, int durationInDays, decimal price);
        Task<ServiceResult<BannerResponse>> CreateForAdminAsync(CreateBannerRequest request, int durationInDays);
        Task<ServiceResult<BannerResponse>> UpdateAsync(long id, UpdateBannerRequest request);
        Task<ServiceResult<string>> DeleteAsync(long id);
        Task<ServiceResult<int>> DeleteExpiredBannersAsync();
    }
}

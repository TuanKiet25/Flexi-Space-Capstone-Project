using AutoMapper;
using FlexiSpace.Application;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public ProfileService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<ProfileResponse>> CreateOrUpdateProfileAsync(ProfileRequest request)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return new ServiceResult<ProfileResponse> { IsSuccess = false, Message = "User not authenticated." };
                }

                var existing = await _unitOfWork.profileRepository.GetAsync(x => x.UserId == userId && !x.IsDeleted);
                if (existing == null)
                {
                    var entity = _mapper.Map<UserProfile>(request);
                    entity.UserId = userId;
                    await _unitOfWork.profileRepository.AddAsync(entity);
                    await _unitOfWork.SaveChangesAsync();
                    var res = _mapper.Map<ProfileResponse>(entity);
                    return new ServiceResult<ProfileResponse> { IsSuccess = true, Data = res };
                }
                existing.FullName = request.FullName ?? existing.FullName;
                existing.AvatarUrl = request.AvatarUrl ?? existing.AvatarUrl;
                existing.Bio = request.Bio ?? existing.Bio;
                existing.SocialLink = request.SocialLink ?? existing.SocialLink;
                existing.Gender = request.Gender;
                existing.UpdatedBy = _currentUserService.UserId ?? existing.UpdatedBy;
                await _unitOfWork.profileRepository.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();
                var mapped = _mapper.Map<ProfileResponse>(existing);
                return new ServiceResult<ProfileResponse> { IsSuccess = true, Data = mapped };
            }
            catch (System.Exception ex)
            {
                return new ServiceResult<ProfileResponse> { IsSuccess = false, Message = ex.Message };
            }
        }


        public async Task<ServiceResult<ProfileResponse>> GetProfileByUserIdAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return new ServiceResult<ProfileResponse> { IsSuccess = false, Message = "UserId is required." };
                var profile = await _unitOfWork.profileRepository.GetAsync(x => x.UserId == userId && !x.IsDeleted);
                if (profile == null) return new ServiceResult<ProfileResponse> { IsSuccess = false, IsNotFound = true, Message = "Profile not found." };
                var mapped = _mapper.Map<ProfileResponse>(profile);
                return new ServiceResult<ProfileResponse> { IsSuccess = true, Data = mapped };
            }
            catch (System.Exception ex)
            {
                return new ServiceResult<ProfileResponse> { IsSuccess = false, Message = ex.Message };
            }
        }
    }
}

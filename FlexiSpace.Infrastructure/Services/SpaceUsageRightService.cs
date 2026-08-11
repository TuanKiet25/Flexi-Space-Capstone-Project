using AutoMapper;
using FlexiSpace.Application;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Infrastructure.Services
{
    public class SpaceUsageRightService : ISpaceUsageRightService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public SpaceUsageRightService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<ServiceResult<SpaceUsageRightResponse>> GrantAsync(GrantSpaceUsageRightRequest request)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<SpaceUsageRightResponse> { IsSuccess = false, Message = "Register first!" };
                }

                var validation = await ValidateGrantAsync(request, currentUserId);
                if (validation.ErrorMessage != null)
                {
                    return new ServiceResult<SpaceUsageRightResponse> { IsSuccess = false, Message = validation.ErrorMessage };
                }

                var usageRight = _mapper.Map<SpaceUsageRight>(request);
                usageRight.GrantedByUserId = currentUserId;
                usageRight.Type = validation.Type;
                usageRight.CanShare = validation.Type == SpaceUsageRightType.PrimaryRenter && request.CanShare;
                usageRight.CanGrantSharePermission = validation.Type == SpaceUsageRightType.PrimaryRenter && request.CanGrantSharePermission;
                usageRight.IsActive = true;
                usageRight.IsDeleted = false;
                usageRight.CreatedAt = DateTime.UtcNow;
                usageRight.CreatedBy = currentUserId;

                await _unitOfWork.spaceUsageRightRepository.AddAsync(usageRight);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<SpaceUsageRightResponse>
                {
                    IsSuccess = true,
                    Message = "Space usage right granted successfully.",
                    Data = _mapper.Map<SpaceUsageRightResponse>(usageRight)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<SpaceUsageRightResponse> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<SpaceUsageRightResponse>> UpdatePermissionAsync(long id, UpdateSpaceUsageRightPermissionRequest request)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<SpaceUsageRightResponse> { IsSuccess = false, Message = "Register first!" };
                }

                var usageRight = await _unitOfWork.spaceUsageRightRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(x => x.Space));
                if (usageRight == null)
                {
                    return new ServiceResult<SpaceUsageRightResponse> { IsSuccess = false, IsNotFound = true, Message = "Space usage right not found." };
                }

                if (usageRight.Space.OwnerId != currentUserId && usageRight.GrantedByUserId != currentUserId)
                {
                    return new ServiceResult<SpaceUsageRightResponse> { IsSuccess = false, Message = "Bạn không có quyền cập nhật quyền sử dụng này." };
                }

                if (usageRight.Type == SpaceUsageRightType.SubRenter)
                {
                    return new ServiceResult<SpaceUsageRightResponse> { IsSuccess = false, Message = "Người thuê phụ không được cấp quyền share tiếp." };
                }

                _mapper.Map(request, usageRight);
                usageRight.UpdatedAt = DateTime.UtcNow;
                usageRight.UpdatedBy = currentUserId;

                await _unitOfWork.spaceUsageRightRepository.UpdateAsync(usageRight);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<SpaceUsageRightResponse>
                {
                    IsSuccess = true,
                    Message = "Space usage permission updated successfully.",
                    Data = _mapper.Map<SpaceUsageRightResponse>(usageRight)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<SpaceUsageRightResponse> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<IEnumerable<SpaceUsageRightResponse>>> GetBySpaceAsync(long spaceId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == spaceId && !x.IsDeleted);
                if (space == null)
                {
                    return new ServiceResult<IEnumerable<SpaceUsageRightResponse>> { IsSuccess = false, IsNotFound = true, Message = "Space not found." };
                }

                if (space.OwnerId != currentUserId)
                {
                    return new ServiceResult<IEnumerable<SpaceUsageRightResponse>> { IsSuccess = false, Message = "Chỉ chủ mặt bằng mới được xem danh sách quyền sử dụng." };
                }

                var rights = await _unitOfWork.spaceUsageRightRepository.GetAllAsync(x => x.SpaceId == spaceId && !x.IsDeleted);
                return new ServiceResult<IEnumerable<SpaceUsageRightResponse>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<IEnumerable<SpaceUsageRightResponse>>(rights)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<SpaceUsageRightResponse>> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<IEnumerable<SpaceUsageRightResponse>>> GetMineAsync(long? spaceId = null)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<IEnumerable<SpaceUsageRightResponse>> { IsSuccess = false, Message = "Register first!" };
                }

                var rights = await _unitOfWork.spaceUsageRightRepository.GetAllAsync(x =>
                    x.UserId == currentUserId &&
                    !x.IsDeleted &&
                    (spaceId == null || x.SpaceId == spaceId.Value));

                return new ServiceResult<IEnumerable<SpaceUsageRightResponse>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<IEnumerable<SpaceUsageRightResponse>>(rights)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<SpaceUsageRightResponse>> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<string>> RevokeAsync(long id)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<string> { IsSuccess = false, Message = "Register first!" };
                }

                var usageRight = await _unitOfWork.spaceUsageRightRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(x => x.Space));
                if (usageRight == null)
                {
                    return new ServiceResult<string> { IsSuccess = false, IsNotFound = true, Message = "Space usage right not found." };
                }

                if (usageRight.Space.OwnerId != currentUserId && usageRight.GrantedByUserId != currentUserId)
                {
                    return new ServiceResult<string> { IsSuccess = false, Message = "Bạn không có quyền thu hồi quyền sử dụng này." };
                }

                usageRight.IsDeleted = true;
                usageRight.IsActive = false;
                usageRight.UpdatedAt = DateTime.UtcNow;
                usageRight.UpdatedBy = currentUserId;

                await _unitOfWork.spaceUsageRightRepository.UpdateAsync(usageRight);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<string> { IsSuccess = true, Data = "Space usage right revoked successfully." };
            }
            catch (Exception ex)
            {
                return new ServiceResult<string> { IsSuccess = false, Message = ex.Message };
            }
        }

        private async Task<(string? ErrorMessage, SpaceUsageRightType Type)> ValidateGrantAsync(GrantSpaceUsageRightRequest request, string currentUserId)
        {
            if (request.ValidFrom >= request.ValidTo)
            {
                return ("Thời gian bắt đầu phải trước thời gian kết thúc.", default);
            }

            if (request.ValidTo.Date < DateTime.UtcNow.Date)
            {
                return ("Thời gian kết thúc không thể nằm trong quá khứ.", default);
            }

            if (request.UserId == currentUserId)
            {
                return ("Bạn không thể tự cấp quyền sử dụng cho chính mình.", default);
            }

            var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == request.SpaceId && !x.IsDeleted && x.IsActive);
            if (space == null)
            {
                return ("Không tìm thấy mặt bằng.", default);
            }

            var targetUser = await _unitOfWork.userRepository.GetAsync(x => x.UserId == request.UserId);
            if (targetUser == null)
            {
                return ("Không tìm thấy người dùng được cấp quyền.", default);
            }

            var validFrom = NormalizeDate(request.ValidFrom);
            var validTo = NormalizeDate(request.ValidTo);
            var isOwner = space.OwnerId == currentUserId;
            if (isOwner)
            {
                var ownerConflict = await HasUsageRightOverlapAsync(request.SpaceId, request.UserId, validFrom, validTo);
                if (ownerConflict)
                {
                    return ("Người dùng này đã có quyền sử dụng mặt bằng trong khoảng thời gian được chọn.", default);
                }

                return (null, SpaceUsageRightType.PrimaryRenter);
            }

            var grantorRight = await GetActiveShareRightAsync(request.SpaceId, currentUserId, validFrom, validTo);
            if (grantorRight == null)
            {
                return ("Bạn không có quyền share mặt bằng này trong khoảng thời gian được chọn.", default);
            }

            var subRenterConflict = await HasUsageRightOverlapAsync(request.SpaceId, request.UserId, validFrom, validTo);
            if (subRenterConflict)
            {
                return ("Người dùng này đã có quyền sử dụng mặt bằng trong khoảng thời gian được chọn.", default);
            }

            return (null, SpaceUsageRightType.SubRenter);
        }

        private async Task<SpaceUsageRight?> GetActiveShareRightAsync(long spaceId, string userId, DateTime validFrom, DateTime validTo)
        {
            var rights = await _unitOfWork.spaceUsageRightRepository.GetAllAsync(x =>
                x.SpaceId == spaceId &&
                x.UserId == userId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.CanShare &&
                x.Type != SpaceUsageRightType.SubRenter &&
                x.ValidFrom <= validFrom &&
                x.ValidTo >= validTo);

            return rights.OrderByDescending(x => x.ValidTo).FirstOrDefault();
        }

        private async Task<bool> HasUsageRightOverlapAsync(long spaceId, string userId, DateTime validFrom, DateTime validTo)
        {
            var conflicts = await _unitOfWork.spaceUsageRightRepository.GetAllAsync(x =>
                x.SpaceId == spaceId &&
                x.UserId == userId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.ValidFrom <= validTo &&
                x.ValidTo >= validFrom);

            return conflicts.Any();
        }

        private static DateTime NormalizeDate(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        }
    }
}

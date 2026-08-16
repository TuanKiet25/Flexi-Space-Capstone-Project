using AutoMapper;
using FlexiSpace.Application;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Application.ViewModels.Responses.Space;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FlexiSpace.Infrastructure.Services
{
    public class SpacePartService : ISpacePartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public SpacePartService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<SpacePartResponse>> CreateAsync(long parentSpaceId, CreateSpacePartRQ request)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<SpacePartResponse> { IsSuccess = false, Message = "Register first!" };
                }

                var parentSpace = await _unitOfWork.spaceRepository.GetAsync(
                    x => x.Id == parentSpaceId && !x.IsDeleted && x.ParentSpaceId == null,
                    include: q => q.Include(s => s.ChildSpaces));

                var validation = await ValidatePartRequestAsync(parentSpace, request, currentUserId);
                if (validation != null)
                {
                    return new ServiceResult<SpacePartResponse> { IsSuccess = false, Message = validation };
                }

                var spacePart = _mapper.Map<Space>(request, opt => opt.Items["ParentSpace"] = parentSpace!);
                spacePart.ParentSpaceId = parentSpaceId;
                spacePart.OwnerId = parentSpace!.OwnerId;
                spacePart.IsDeleted = false;
                spacePart.CreatedBy = currentUserId;
                spacePart.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.spaceRepository.AddAsync(spacePart);
                await _unitOfWork.SaveChangesAsync();

                var created = await LoadPartAsync(spacePart.Id);
                return new ServiceResult<SpacePartResponse>
                {
                    IsSuccess = true,
                    Message = "Space part created successfully.",
                    Data = _mapper.Map<SpacePartResponse>(created)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<SpacePartResponse> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<IEnumerable<SpacePartResponse>>> CreateManyAsync(long parentSpaceId, CreateSpacePartsRQ request)
        {
            var transactionStarted = false;
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<IEnumerable<SpacePartResponse>> { IsSuccess = false, Message = "Register first!" };
                }

                var parentSpace = await _unitOfWork.spaceRepository.GetAsync(
                    x => x.Id == parentSpaceId && !x.IsDeleted && x.ParentSpaceId == null,
                    include: q => q.Include(s => s.ChildSpaces));

                var validation = await ValidatePartRequestsAsync(parentSpace, request, currentUserId);
                if (validation != null)
                {
                    return new ServiceResult<IEnumerable<SpacePartResponse>> { IsSuccess = false, Message = validation };
                }

                await _unitOfWork.BeginTransactionAsync();
                transactionStarted = true;

                var spaceParts = request.Parts
                    .Select(partRequest =>
                    {
                        var spacePart = _mapper.Map<Space>(partRequest, opt => opt.Items["ParentSpace"] = parentSpace!);
                        spacePart.ParentSpaceId = parentSpaceId;
                        spacePart.OwnerId = parentSpace!.OwnerId;
                        spacePart.IsDeleted = false;
                        spacePart.CreatedBy = currentUserId;
                        spacePart.CreatedAt = DateTime.UtcNow;
                        return spacePart;
                    })
                    .ToList();

                await _unitOfWork.spaceRepository.AddRangeAsync(spaceParts);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                transactionStarted = false;

                var createdIds = spaceParts.Select(x => x.Id).ToList();
                var createdParts = await LoadPartsAsync(createdIds);

                return new ServiceResult<IEnumerable<SpacePartResponse>>
                {
                    IsSuccess = true,
                    Message = "Space parts created successfully.",
                    Data = _mapper.Map<IEnumerable<SpacePartResponse>>(createdParts)
                };
            }
            catch (Exception ex)
            {
                if (transactionStarted)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                return new ServiceResult<IEnumerable<SpacePartResponse>> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<IEnumerable<SpacePartResponse>>> GetByParentSpaceAsync(long parentSpaceId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<IEnumerable<SpacePartResponse>> { IsSuccess = false, Message = "Register first!" };
                }

                var parentSpace = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == parentSpaceId && !x.IsDeleted);
                if (parentSpace == null)
                {
                    return new ServiceResult<IEnumerable<SpacePartResponse>> { IsSuccess = false, IsNotFound = true, Message = "Parent space not found." };
                }

                var parts = await _unitOfWork.spaceRepository.GetAllAsync(
                    x => x.ParentSpaceId == parentSpaceId && !x.IsDeleted && x.CreatedBy == currentUserId,
                    include: q => q.Include(s => s.ParentSpace)
                                   .Include(s => s.Amenity)
                                   .Include(s => s.OperatingHour)
                                   .Include(s => s.SpaceAllowedCategory)
                                   .Include(s => s.PictureURL));

                return new ServiceResult<IEnumerable<SpacePartResponse>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<IEnumerable<SpacePartResponse>>(parts)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<SpacePartResponse>> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<SpacePartResponse>> GetByIdAsync(long id)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<SpacePartResponse> { IsSuccess = false, Message = "Register first!" };
                }

                var part = await LoadPartAsync(id, currentUserId);
                if (part == null)
                {
                    return new ServiceResult<SpacePartResponse> { IsSuccess = false, IsNotFound = true, Message = "Space part not found." };
                }

                return new ServiceResult<SpacePartResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<SpacePartResponse>(part)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<SpacePartResponse> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<SpacePartResponse>> UpdateAsync(long id, UpdateSpacePartRQ request)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<SpacePartResponse> { IsSuccess = false, Message = "Register first!" };
                }

                var existingPart = await _unitOfWork.spaceRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted && x.ParentSpaceId != null && x.CreatedBy == currentUserId,
                    include: q => q.Include(s => s.ParentSpace)
                                   .Include(s => s.Amenity)
                                   .Include(s => s.OperatingHour)
                                   .Include(s => s.SpaceAllowedCategory));

                if (existingPart == null)
                {
                    return new ServiceResult<SpacePartResponse> { IsSuccess = false, IsNotFound = true, Message = "Space part not found." };
                }

                var validation = await ValidatePartRequestAsync(existingPart.ParentSpace, request, currentUserId, existingPart.Id);
                if (validation != null)
                {
                    return new ServiceResult<SpacePartResponse> { IsSuccess = false, Message = validation };
                }

                _mapper.Map(request, existingPart, opt => opt.Items["ParentSpace"] = existingPart.ParentSpace);
                existingPart.UpdatedBy = currentUserId;
                existingPart.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.spaceRepository.UpdateAsync(existingPart);
                await _unitOfWork.SaveChangesAsync();

                var updated = await LoadPartAsync(id);
                return new ServiceResult<SpacePartResponse>
                {
                    IsSuccess = true,
                    Message = "Space part updated successfully.",
                    Data = _mapper.Map<SpacePartResponse>(updated)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<SpacePartResponse> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<string>> DeleteAsync(long id)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<string> { IsSuccess = false, Message = "Register first!" };
                }

                var existingPart = await _unitOfWork.spaceRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted && x.ParentSpaceId != null && x.CreatedBy == currentUserId,
                    include: q => q.Include(s => s.ParentSpace));
                if (existingPart == null)
                {
                    return new ServiceResult<string> { IsSuccess = false, IsNotFound = true, Message = "Space part not found." };
                }

                var hasActiveListing = (await _unitOfWork.listingRepository.GetAllAsync(
                    x => x.SpaceId == existingPart.Id && !x.IsDeleted && x.IsActive)).Any();
                var hasActiveContract = (await _unitOfWork.contractRepository.GetAllAsync(
                    x => x.SpaceId == existingPart.Id && !x.IsDeleted && x.Status == FlexiSpace.Domain.Enum.ContractStatusEnum.Active)).Any();

                if (hasActiveListing || hasActiveContract)
                {
                    return new ServiceResult<string> { IsSuccess = false, Message = "Cannot delete a space part that has active listings or contracts." };
                }

                existingPart.IsDeleted = true;
                existingPart.IsActive = false;
                existingPart.UpdatedBy = currentUserId;
                existingPart.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.spaceRepository.UpdateAsync(existingPart);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<string> { IsSuccess = true, Data = "Space part deleted successfully." };
            }
            catch (Exception ex)
            {
                return new ServiceResult<string> { IsSuccess = false, Message = ex.Message };
            }
        }

        private async Task<string?> ValidatePartRequestAsync(Space? parentSpace, CreateSpacePartRQ request, string currentUserId, long? excludedPartId = null)
        {
            if (parentSpace == null)
            {
                return "Parent space not found.";
            }

            if (parentSpace.OwnerId != currentUserId && !await HasActiveShareRightAsync(parentSpace.Id, currentUserId))
            {
                return "Only the parent space owner or a user with share permission can manage space parts.";
            }

            var basicValidation = await ValidatePartBasicInfoAsync(request);
            if (basicValidation != null) return basicValidation;

            var siblingParts = await _unitOfWork.spaceRepository.GetAllAsync(
                x => x.ParentSpaceId == parentSpace.Id && !x.IsDeleted && (excludedPartId == null || x.Id != excludedPartId.Value));

            var totalArea = siblingParts.Sum(x => x.Area) + request.Area;
            if (totalArea > parentSpace.Area)
            {
                return $"Total area of active space parts ({totalArea}) cannot exceed parent space area ({parentSpace.Area}).";
            }

            return null;
        }

        private async Task<string?> ValidatePartBasicInfoAsync(CreateSpacePartRQ request)
        {
            if (request.Area <= 0)
            {
                return "Area must be greater than zero.";
            }

            if (request.OperatingHours != null && request.OperatingHours.Any(oh => oh.OpenTime >= oh.CloseTime))
            {
                return "Operating hours are invalid. Open time must be before close time.";
            }

            if (request.OperatingHours != null && request.OperatingHours.Any(oh => oh.DayOfWeek < 0 || oh.DayOfWeek > 6))
            {
                return "Operating hours are invalid. Day of week must be between 0 (Sunday) and 6 (Saturday).";
            }

            if (request.SpaceAllowedCategories != null && request.SpaceAllowedCategories.Any())
            {
                var categoryIds = request.SpaceAllowedCategories
                    .Where(c => c.BussinessCategoryId.HasValue)
                    .Select(c => c.BussinessCategoryId!.Value)
                    .Distinct()
                    .ToList();

                if (!categoryIds.Any()) return null;

                var existedCategories = await _unitOfWork.bussinessCategoryRepository
                    .GetAllAsync(x => categoryIds.Contains(x.Id));
                if (existedCategories.Count != categoryIds.Count)
                {
                    var invalidIds = categoryIds.Except(existedCategories.Select(e => e.Id)).ToList();
                    return $"Not found SpaceAllowedCategories with IDs: {string.Join(", ", invalidIds)}.";
                }
            }

            return null;
        }

        private async Task<string?> ValidatePartRequestsAsync(Space? parentSpace, CreateSpacePartsRQ request, string currentUserId)
        {
            if (parentSpace == null)
            {
                return "Parent space not found.";
            }

            if (parentSpace.OwnerId != currentUserId && !await HasActiveShareRightAsync(parentSpace.Id, currentUserId))
            {
                return "Only the parent space owner or a user with share permission can manage space parts.";
            }

            if (request.Parts == null || !request.Parts.Any())
            {
                return "Please provide at least one space part.";
            }

            for (var index = 0; index < request.Parts.Count; index++)
            {
                var part = request.Parts[index];
                var validation = await ValidatePartBasicInfoAsync(part);
                if (validation != null)
                {
                    return $"Part #{index + 1}: {validation}";
                }
            }

            var siblingParts = await _unitOfWork.spaceRepository.GetAllAsync(
                x => x.ParentSpaceId == parentSpace.Id && !x.IsDeleted);

            var totalArea = siblingParts.Sum(x => x.Area) + request.Parts.Sum(x => x.Area);
            if (totalArea > parentSpace.Area)
            {
                return $"Total area of active space parts ({totalArea}) cannot exceed parent space area ({parentSpace.Area}).";
            }

            return null;
        }

        private async Task<Space?> LoadPartAsync(long id)
        {
            return await _unitOfWork.spaceRepository.GetAsync(
                x => x.Id == id && !x.IsDeleted && x.ParentSpaceId != null,
                include: q => q.Include(s => s.ParentSpace)
                               .Include(s => s.Amenity)
                               .Include(s => s.OperatingHour)
                               .Include(s => s.SpaceAllowedCategory)
                               .Include(s => s.PictureURL));
        }

        private async Task<Space?> LoadPartAsync(long id, string createdBy)
        {
            return await _unitOfWork.spaceRepository.GetAsync(
                x => x.Id == id && !x.IsDeleted && x.ParentSpaceId != null && x.CreatedBy == createdBy,
                include: q => q.Include(s => s.ParentSpace)
                               .Include(s => s.Amenity)
                               .Include(s => s.OperatingHour)
                               .Include(s => s.SpaceAllowedCategory)
                               .Include(s => s.PictureURL));
        }

        private async Task<List<Space>> LoadPartsAsync(List<long> ids)
        {
            return await _unitOfWork.spaceRepository.GetAllAsync(
                x => ids.Contains(x.Id) && !x.IsDeleted && x.ParentSpaceId != null,
                include: q => q.Include(s => s.ParentSpace)
                               .Include(s => s.Amenity)
                               .Include(s => s.OperatingHour)
                               .Include(s => s.SpaceAllowedCategory)
                               .Include(s => s.PictureURL));
        }

        private async Task<bool> HasActiveShareRightAsync(long spaceId, string userId)
        {
            var now = DateTime.UtcNow;
            var rights = await _unitOfWork.spaceUsageRightRepository.GetAllAsync(x =>
                x.SpaceId == spaceId &&
                x.UserId == userId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.CanShare &&
                x.Type != SpaceUsageRightType.SubRenter &&
                x.ValidFrom <= now &&
                x.ValidTo >= now);

            return rights.Any();
        }
    }
}

using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Application.ViewModels.Responses.Space;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Infrastructure.Services;
using FlexiSpace.Infrastructure.Helper;
using FlexiSpace.Infrastructure;

namespace FlexiSpace.Application.Services
{
    public class SpaceService : ISpaceService
    {
        private readonly ISpaceRepository _spaceRepository;
        private readonly IMapper _mapper;
        private readonly IInsertAndUpdate<Space, OperatingHour> insertAndUpdateOperatingHours;
        private readonly IUnitOfWork _unitOfWork;
        public SpaceService(ISpaceRepository spaceRepository, IMapper mapper, IInsertAndUpdate<Space, OperatingHour> insertAndUpdateOperatingHours, IUnitOfWork unitOfWork)
        {
            _spaceRepository = spaceRepository;
            _mapper = mapper;
            this.insertAndUpdateOperatingHours = insertAndUpdateOperatingHours;
            _unitOfWork = unitOfWork;
        }

        private string? ValidateCreateSpaceRQ(CreateSpaceRQ space)
        {
            try
            {
                string NullAddressOrCity = "Address and City cannot be null or empty.";
                string InvalidArea = "Area must be greater than zero.";
                string InvalidOperatingHours_OpenTime = "Operating hours are invalid. Open time must be before close time.";
                string InvalidOperatingHours_DayOfWeek = "Operating hours are invalid. Day of week must be between 0 (Sunday) and 6 (Saturday).";

                if(space.SpaceAmenities != null && space.SpaceAmenities.Any())
                    foreach (var amenity in space.SpaceAmenities)
                    {
                        if (_unitOfWork.spaceAmenityRepository.GetAsync(x => x.AmenityId == amenity.AmenityId) == null)
                            return "Failed to initialize space amenity repository.";
                    }
                if(space.SpaceAllowedCategories != null && space.SpaceAllowedCategories.Any())
                    foreach (var category in space.SpaceAllowedCategories)
                    {
                        if (_unitOfWork.spaceAllowedCategoryRepository.GetAsync(x => x.BussinessCategoryId == category.BussinessCategoryId) == null)
                            return "Failed to initialize space allowed category repository.";
                    }
                if (string.IsNullOrEmpty(space.Address) || string.IsNullOrEmpty(space.City))
                    return NullAddressOrCity;
                if (space.Area <= 0)
                    return InvalidArea;
                if (space.OperatingHours != null && space.OperatingHours.Any(oh => oh.OpenTime >= oh.CloseTime))
                    return InvalidOperatingHours_OpenTime;
                if (space.OperatingHours != null && space.OperatingHours.Any(oh => oh.DayOfWeek < 0 || oh.DayOfWeek > 6))
                    return InvalidOperatingHours_DayOfWeek;

                return null;
            }catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<ServiceResult<CreateSpaceRQ>> Create(CreateSpaceRQ space)
        {
            try
            {
                var validationError = ValidateCreateSpaceRQ(space);
                if (validationError != null)
                {
                    return new ServiceResult<CreateSpaceRQ>
                    {
                        IsSuccess = false,
                        Message = validationError
                    };
                }

                space.OwnerId = GlobalVariables.CurrentUserId;

                var parentSpace = _mapper.Map<CreateSpaceRQ, Space>(space);

                var insertResult = await insertAndUpdateOperatingHours.Insert(parentSpace, [..parentSpace.OperatingHour]);
                if (!insertResult.IsSuccess)
                {
                    return new ServiceResult<CreateSpaceRQ>
                    {
                        IsSuccess = false,
                        Message = insertResult.Message ?? "Failed to create space."
                    };
                }

                return new ServiceResult<CreateSpaceRQ>
                {
                    IsSuccess = true,
                    Data = space,
                    Message = "Space created successfully."
                };
            }
            catch
            {
                return new ServiceResult<CreateSpaceRQ>
                {
                    IsSuccess = false,
                    Message = "Failed to create space."
                };
            }
        }

        public async Task<ServiceResult<IEnumerable<GetAllSpace>>> GetAll(FilterGetAllSpace filter)
        {
            try
            {
                var spaces = await _spaceRepository.GetAllAsync(
                    filter: x => (string.IsNullOrEmpty(filter.OwnerId) || x.OwnerId == filter.OwnerId) &&
                                (string.IsNullOrEmpty(filter.Address) || x.Address.Contains(filter.Address)) &&
                                (string.IsNullOrEmpty(filter.City) || x.City.Contains(filter.City)) &&
                                (filter.Area <= 0 || x.Area >= filter.Area),
                    include: x => x.Include(s => s.Owner)
                              .Include(s => s.PrimaryBookingRequest)
                              .Include(s => s.Listing)
                              .Include(s => s.SpaceAmenity)
                              .Include(s => s.OperatingHour)
                              .Include(s => s.SpaceAllowedCategory)
                    );
                var mappedSpaces = _mapper.Map<IEnumerable<GetAllSpace>>(spaces);
                return new ServiceResult<IEnumerable<GetAllSpace>>
                {
                    IsSuccess = true,
                    Data = mappedSpaces
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<GetAllSpace>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
    }
}

using AutoMapper;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels;
using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Application.ViewModels.Responses.Space;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Infrastructure;
using FlexiSpace.Infrastructure.Helper;
using FlexiSpace.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FlexiSpace.Application.Services
{
    public class SpaceService : ISpaceService
    {
        private const string VietnamAddressFileName = "vietnam_address.json";
        private static readonly SemaphoreSlim AddressCacheLock = new(1, 1);
        private static IReadOnlyList<AddressNodeRP>? _addressCache;

        private readonly ISpaceRepository _spaceRepository;
        private readonly IMapper _mapper;
        private readonly IInsertAndUpdate<Space, OperatingHour> insertAndUpdateOperatingHours;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPictureURL _mediaService;

        public SpaceService(
            ISpaceRepository spaceRepository,
            IMapper mapper,
            IInsertAndUpdate<Space, OperatingHour> insertAndUpdateOperatingHours,
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostEnvironment,
            IPictureURL mediaService)
        {
            _spaceRepository = spaceRepository;
            _mapper = mapper;
            this.insertAndUpdateOperatingHours = insertAndUpdateOperatingHours;
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _mediaService = mediaService;
        }

        private async Task<IReadOnlyList<AddressNodeRP>> GetAddressCacheAsync()
        {
            if (_addressCache != null)
            {
                return _addressCache;
            }

            await AddressCacheLock.WaitAsync();
            try
            {
                if (_addressCache != null)
                {
                    return _addressCache;
                }

                var filePath = Path.Combine(_hostEnvironment.ContentRootPath, "SeedData", VietnamAddressFileName);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Address seed file was not found.", filePath);
                }

                var jsonContent = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var addresses = JsonSerializer.Deserialize<List<AddressNodeRP>>(jsonContent, options) ?? new List<AddressNodeRP>();
                _addressCache = addresses;

                return _addressCache;
            }
            finally
            {
                AddressCacheLock.Release();
            }
        }

        public async Task<ServiceResult<IEnumerable<AddressOptionRP>>> GetAddress(string? provinceCode, string? districtCode)
        {
            try
            {
                var addresses = await GetAddressCacheAsync();

                if (string.IsNullOrWhiteSpace(provinceCode))
                {
                    return new ServiceResult<IEnumerable<AddressOptionRP>>
                    {
                        IsSuccess = true,
                        Data = addresses.Select(x => new AddressOptionRP
                        {
                            Value = x.Value,
                            Label = x.Label
                        }).ToList()
                    };
                }

                var province = addresses.FirstOrDefault(x => x.Value == provinceCode);
                if (province == null)
                {
                    return new ServiceResult<IEnumerable<AddressOptionRP>>
                    {
                        IsSuccess = false,
                        Message = "Province not found."
                    };
                }

                if (string.IsNullOrWhiteSpace(districtCode))
                {
                    return new ServiceResult<IEnumerable<AddressOptionRP>>
                    {
                        IsSuccess = true,
                        Data = province.Children.Select(x => new AddressOptionRP
                        {
                            Value = x.Value,
                            Label = x.Label
                        }).ToList()
                    };
                }

                var district = province.Children.FirstOrDefault(x => x.Value == districtCode);
                if (district == null)
                {
                    return new ServiceResult<IEnumerable<AddressOptionRP>>
                    {
                        IsSuccess = false,
                        Message = "District not found."
                    };
                }

                return new ServiceResult<IEnumerable<AddressOptionRP>>
                {
                    IsSuccess = true,
                    Data = district.Children.Select(x => new AddressOptionRP
                    {
                        Value = x.Value,
                        Label = x.Label
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<AddressOptionRP>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        private async Task<string?> ValidateCreateSpaceRQ(CreateSpaceRQ space)
        {
            try
            {
                string NullAddressOrCity = "Address and City cannot be null or empty.";
                string InvalidArea = "Area must be greater than zero.";
                string InvalidOperatingHours_OpenTime = "Operating hours are invalid. Open time must be before close time.";
                string InvalidOperatingHours_DayOfWeek = "Operating hours are invalid. Day of week must be between 0 (Sunday) and 6 (Saturday).";

                //them validate cho Amentity

                if (space.SpaceAllowedCategories != null && space.SpaceAllowedCategories.Any())
                    foreach (var category in space.SpaceAllowedCategories)
                    {
                        var name = await _unitOfWork.bussinessCategoryRepository.GetAsync(x => x.Id == category.BussinessCategoryId);
                        if (name == null)
                        {
                            return $"Not found spaceAllowedCategories {category.BussinessCategoryId}.";
                        }
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
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void ReplaceSpaceChildren(Space existingSpace, CreateSpaceRQ space)
        {
            if (space.Amenities != null)
            {
                existingSpace.Amenity ??= new List<Amentity>();
                existingSpace.Amenity.Clear();
                foreach (var amenity in _mapper.Map<List<Amentity>>(space.Amenities))
                {
                    existingSpace.Amenity.Add(amenity);
                }
            }

            if (space.OperatingHours != null)
            {
                existingSpace.OperatingHour.Clear();
                foreach (var operatingHour in _mapper.Map<List<OperatingHour>>(space.OperatingHours))
                {
                    existingSpace.OperatingHour.Add(operatingHour);
                }
            }

            if (space.SpaceAllowedCategories != null)
            {
                existingSpace.SpaceAllowedCategory.Clear();
                foreach (var category in _mapper.Map<List<SpaceAllowedCategory>>(space.SpaceAllowedCategories))
                {
                    existingSpace.SpaceAllowedCategory.Add(category);
                }
            }
        }

        public async Task<ServiceResult<CreateSpaceRP>> Create(CreateSpaceRQ space)
        {
            try
            {
                var validationError = await ValidateCreateSpaceRQ(space);
                string a = validationError ?? string.Empty;
                if (validationError != null)
                {
                    return new ServiceResult<CreateSpaceRP>
                    {
                        IsSuccess = false,
                        Message = validationError
                    };
                }

                space.OwnerId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                var parentSpace = _mapper.Map<CreateSpaceRQ, Space>(space);

                var insertResult = await insertAndUpdateOperatingHours.Insert(parentSpace, [..parentSpace.OperatingHour]);


                if (!insertResult.IsSuccess)
                {
                    return new ServiceResult<CreateSpaceRP>
                    {
                        IsSuccess = false,
                        Message = insertResult.Message ?? "Failed to create space."
                    };
                }
                
                var result = _mapper.Map<CreateSpaceRQ, CreateSpaceRP>(space);


                if (space.PictureURLs != null && space.PictureURLs.Any() && insertResult.Data != null)
                {
                    List<PictureURLVModel> uploadedImages = await _mediaService.UploadImagesAsync(space.PictureURLs, insertResult.Data.Id);

                    if (uploadedImages.Any())
                    {
                        uploadedImages.First().IsPrimary = true;
                    }
                }

                return new ServiceResult<CreateSpaceRP>
                {
                    IsSuccess = true,
                    Data = result,
                    Message = "Space created successfully."
                };
            }
            catch
            {
                return new ServiceResult<CreateSpaceRP>
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
                    filter: x => !x.IsDeleted &&
                                (string.IsNullOrEmpty(filter.OwnerId) || x.OwnerId == filter.OwnerId) &&
                                (string.IsNullOrEmpty(filter.Address) || x.Address.Contains(filter.Address)) &&
                                (string.IsNullOrEmpty(filter.City) || x.City.Contains(filter.City)) &&
                                (filter.Area <= 0 || x.Area >= filter.Area),
                    include: x => x.Include(s => s.Owner)
                              .Include(s => s.PrimaryBookingRequest)
                              .Include(s => s.Listing)
                              .Include(s => s.Amenity)
                              .Include(s => s.OperatingHour)
                              .Include(s => s.SpaceAllowedCategory)
                              .Include(s => s.PictureURL)
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

        public async Task<ServiceResult<GetSpaceByIdRP>> GetById(long id)
        {
            try
            {
                var space = await _spaceRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: x => x.Include(s => s.Owner)
                              .Include(s => s.PrimaryBookingRequest)
                              .Include(s => s.Listing)
                              .Include(s => s.Amenity)
                              .Include(s => s.OperatingHour)
                              .Include(s => s.SpaceAllowedCategory));

                if (space == null)
                {
                    return new ServiceResult<GetSpaceByIdRP>
                    {
                        IsSuccess = false,
                        Message = "Space not found."
                    };
                }

                return new ServiceResult<GetSpaceByIdRP>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<GetSpaceByIdRP>(space)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<GetSpaceByIdRP>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<GetSpaceByIdRP>> Update(long id, CreateSpaceRQ space)
        {
            try
            {
                var validationError = await ValidateCreateSpaceRQ(space);
                if (validationError != null)
                {
                    return new ServiceResult<GetSpaceByIdRP>
                    {
                        IsSuccess = false,
                        Message = validationError
                    };
                }

                var existingSpace = await _spaceRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: x => x.Include(s => s.Owner)
                              .Include(s => s.PrimaryBookingRequest)
                              .Include(s => s.Listing)
                              .Include(s => s.Amenity)
                              .Include(s => s.OperatingHour)
                              .Include(s => s.SpaceAllowedCategory));

                if (existingSpace == null)
                {
                    return new ServiceResult<GetSpaceByIdRP>
                    {
                        IsSuccess = false,
                        Message = "Space not found."
                    };
                }

                existingSpace.Name = space.Name ?? existingSpace.Name;
                existingSpace.Address = space.Address ?? existingSpace.Address;
                existingSpace.City = space.City ?? existingSpace.City;
                existingSpace.Area = space.Area;
                existingSpace.IsActive = space.IsActive;
                existingSpace.IsDeleted = space.IsDeleted;
                existingSpace.UpdatedBy = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                existingSpace.UpdatedAt = DateTime.Now;

                ReplaceSpaceChildren(existingSpace, space);

                await _spaceRepository.UpdateAsync(existingSpace);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<GetSpaceByIdRP>
                {
                    IsSuccess = true,
                    Message = "Space updated successfully.",
                    Data = _mapper.Map<GetSpaceByIdRP>(existingSpace)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<GetSpaceByIdRP>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<string>> Delete(long id)
        {
            try
            {
                var existingSpace = await _spaceRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (existingSpace == null)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "Space not found."
                    };
                }

                existingSpace.IsDeleted = true;
                existingSpace.IsActive = false;
                existingSpace.UpdatedBy = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                existingSpace.UpdatedAt = DateTime.Now;

                await _spaceRepository.UpdateAsync(existingSpace);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<string>
                {
                    IsSuccess = true,
                    Data = "Space deleted successfully."
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
    }
}

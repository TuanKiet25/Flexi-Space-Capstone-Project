using AutoMapper;
using FlexiSpace.Application;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests.PriorityLevelRQ;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Services
{
    public class PriorityLevelService : IPriorityLevelService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public PriorityLevelService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        private string? ValidateCreatePriorityLevelRQ(CreatePriorityLevel priorityLevel)
        {
            try
            {
                if (string.IsNullOrEmpty(priorityLevel.Name))
                    return "Name cannot be null or empty.";
                if (priorityLevel.Price < 0)
                    return "Price cannot be negative.";
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<ServiceResult<CreatePriorityLevel>> Create(CreatePriorityLevel priorityLevel)
        {
            try
            {
                var validationMessage = ValidateCreatePriorityLevelRQ(priorityLevel);
                if (validationMessage != null)
                {
                    return new ServiceResult<CreatePriorityLevel>
                    {
                        IsSuccess = false,
                        Message = validationMessage
                    };
                }

                priorityLevel.CreatedBy = _currentUserService.UserId ?? "System";

                var result = _mapper.Map<CreatePriorityLevel, PriorityLevel>(priorityLevel);
                result.IsActive = priorityLevel.IsActive ?? true;

                await _unitOfWork.priorityLevelRepository.AddAsync(result);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<CreatePriorityLevel>
                {
                    IsSuccess = true,
                    Data = priorityLevel
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<CreatePriorityLevel>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<List<GetAllPriorityLevel>>> GetAll(FilterGetAllPriorityLevel filter)
        {
            try
            {
                var priorityLevels = await _unitOfWork.priorityLevelRepository.GetAllAsync(
                    filter: x => !x.IsDeleted &&
                                (string.IsNullOrEmpty(filter.Name) || x.Name.Contains(filter.Name))
                );

                var mappedResult = _mapper.Map<List<GetAllPriorityLevel>>(priorityLevels);
                return new ServiceResult<List<GetAllPriorityLevel>>
                {
                    IsSuccess = true,
                    Data = mappedResult
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<GetAllPriorityLevel>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<GetAllPriorityLevel>> GetById(long id)
        {
            try
            {
                var priorityLevel = await _unitOfWork.priorityLevelRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (priorityLevel == null)
                {
                    return new ServiceResult<GetAllPriorityLevel>
                    {
                        IsSuccess = false,
                        Message = "Priority level not found."
                    };
                }

                var mappedResult = _mapper.Map<GetAllPriorityLevel>(priorityLevel);
                return new ServiceResult<GetAllPriorityLevel>
                {
                    IsSuccess = true,
                    Data = mappedResult
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<GetAllPriorityLevel>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<GetAllPriorityLevel>> Update(long id, CreatePriorityLevel priorityLevel)
        {
            try
            {
                var validationMessage = ValidateCreatePriorityLevelRQ(priorityLevel);
                if (validationMessage != null)
                {
                    return new ServiceResult<GetAllPriorityLevel>
                    {
                        IsSuccess = false,
                        Message = validationMessage
                    };
                }

                var existingPriority = await _unitOfWork.priorityLevelRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (existingPriority == null)
                {
                    return new ServiceResult<GetAllPriorityLevel>
                    {
                        IsSuccess = false,
                        Message = "Priority level not found."
                    };
                }

                existingPriority.Name = priorityLevel.Name ?? existingPriority.Name;
                existingPriority.Price = priorityLevel.Price;
                existingPriority.IsActive = priorityLevel.IsActive ?? existingPriority.IsActive;
                existingPriority.UpdatedBy = _currentUserService.UserId ?? "System";
                existingPriority.UpdatedAt = DateTime.Now;

                await _unitOfWork.priorityLevelRepository.UpdateAsync(existingPriority);
                await _unitOfWork.SaveChangesAsync();

                var mappedResult = _mapper.Map<GetAllPriorityLevel>(existingPriority);
                return new ServiceResult<GetAllPriorityLevel>
                {
                    IsSuccess = true,
                    Message = "Priority level updated successfully.",
                    Data = mappedResult
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<GetAllPriorityLevel>
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
                var existingPriority = await _unitOfWork.priorityLevelRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (existingPriority == null)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "Priority level not found."
                    };
                }

                existingPriority.IsDeleted = true;
                existingPriority.IsActive = false;
                existingPriority.UpdatedBy = _currentUserService.UserId ?? "System";
                existingPriority.UpdatedAt = DateTime.Now;

                await _unitOfWork.priorityLevelRepository.UpdateAsync(existingPriority);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<string>
                {
                    IsSuccess = true,
                    Data = "Priority level deleted successfully."
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

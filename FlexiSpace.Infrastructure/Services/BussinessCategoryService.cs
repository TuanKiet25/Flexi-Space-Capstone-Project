using AutoMapper;
using FlexiSpace.Application;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests.BussinessCategoryRQ;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Services
{
    public class BussinessCategoryService : IBussinessCategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public BussinessCategoryService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        private string? ValidateCreateBussinessCategoryRQ(CreateBussinessCategory bussinessCategory)
        {
            try
            {
                string NullName = "Name cannot be null or empty.";
                if (string.IsNullOrEmpty(bussinessCategory.Name))
                    return NullName;
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<ServiceResult<CreateBussinessCategory>> Create(CreateBussinessCategory bussinessCategory)
        {
            try
            {
                var validationMessage = ValidateCreateBussinessCategoryRQ(bussinessCategory);
                if (validationMessage != null)
                {
                    return new ServiceResult<CreateBussinessCategory>
                    {
                        IsSuccess = false,
                        Message = validationMessage
                    };
                }

                bussinessCategory.CreatedBy = _currentUserService.UserId ?? "System";

                var result = _mapper.Map<CreateBussinessCategory, BussinessCategory>(bussinessCategory);
                result.IsActive = bussinessCategory.IsActive ?? true;

                await _unitOfWork.bussinessCategoryRepository.AddAsync(result);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<CreateBussinessCategory>
                {
                    IsSuccess = true,
                    Data = bussinessCategory
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<CreateBussinessCategory>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<List<GetAllBussinessCategory>>> GetAll(FilterGetAllBussinessCategory filter)
        {
            try
            {
                var categories = await _unitOfWork.bussinessCategoryRepository.GetAllAsync(
                    filter: x => !x.IsDeleted &&
                                (string.IsNullOrEmpty(filter.Name) || x.Name.Contains(filter.Name))
                );

                var mappedResult = _mapper.Map<List<GetAllBussinessCategory>>(categories);
                return new ServiceResult<List<GetAllBussinessCategory>>
                {
                    IsSuccess = true,
                    Data = mappedResult
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<GetAllBussinessCategory>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<GetAllBussinessCategory>> GetById(long id)
        {
            try
            {
                var category = await _unitOfWork.bussinessCategoryRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (category == null)
                {
                    return new ServiceResult<GetAllBussinessCategory>
                    {
                        IsSuccess = false,
                        Message = "Business category not found."
                    };
                }

                var mappedResult = _mapper.Map<GetAllBussinessCategory>(category);
                return new ServiceResult<GetAllBussinessCategory>
                {
                    IsSuccess = true,
                    Data = mappedResult
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<GetAllBussinessCategory>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<GetAllBussinessCategory>> Update(long id, CreateBussinessCategory bussinessCategory)
        {
            try
            {
                var validationMessage = ValidateCreateBussinessCategoryRQ(bussinessCategory);
                if (validationMessage != null)
                {
                    return new ServiceResult<GetAllBussinessCategory>
                    {
                        IsSuccess = false,
                        Message = validationMessage
                    };
                }

                var existingCategory = await _unitOfWork.bussinessCategoryRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (existingCategory == null)
                {
                    return new ServiceResult<GetAllBussinessCategory>
                    {
                        IsSuccess = false,
                        Message = "Business category not found."
                    };
                }

                existingCategory.Name = bussinessCategory.Name ?? existingCategory.Name;
                existingCategory.IsActive = bussinessCategory.IsActive ?? existingCategory.IsActive;
                existingCategory.UpdatedBy = _currentUserService.UserId ?? "System";
                existingCategory.UpdatedAt = DateTime.Now;

                await _unitOfWork.bussinessCategoryRepository.UpdateAsync(existingCategory);
                await _unitOfWork.SaveChangesAsync();

                var mappedResult = _mapper.Map<GetAllBussinessCategory>(existingCategory);
                return new ServiceResult<GetAllBussinessCategory>
                {
                    IsSuccess = true,
                    Message = "Business category updated successfully.",
                    Data = mappedResult
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<GetAllBussinessCategory>
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
                var existingCategory = await _unitOfWork.bussinessCategoryRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (existingCategory == null)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "Business category not found."
                    };
                }

                existingCategory.IsDeleted = true;
                existingCategory.IsActive = false;
                existingCategory.UpdatedBy = _currentUserService.UserId ?? "System";
                existingCategory.UpdatedAt = DateTime.Now;

                await _unitOfWork.bussinessCategoryRepository.UpdateAsync(existingCategory);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<string>
                {
                    IsSuccess = true,
                    Data = "Business category deleted successfully."
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

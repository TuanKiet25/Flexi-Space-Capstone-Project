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
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BussinessCategoryService(IServiceProvider serviceProvider, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _serviceProvider = serviceProvider;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

                bussinessCategory.CreatedBy = GlobalVariables.CurrentUserId;

                var result = _mapper.Map<CreateBussinessCategory, BussinessCategory>(bussinessCategory);

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

        public Task<ServiceResult<List<GetAllBussinessCategory>>> GetAll(FilterGetAllBussinessCategory filter)
        {
            throw new NotImplementedException();
        }
    }
}

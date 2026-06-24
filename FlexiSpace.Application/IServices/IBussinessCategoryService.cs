using FlexiSpace.Application.ViewModels.Requests.BussinessCategoryRQ;
using FlexiSpace.Application.ViewModels.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IBussinessCategoryService
    {
        Task<ServiceResult<List<GetAllBussinessCategory>>> GetAll(FilterGetAllBussinessCategory filter);
        Task<ServiceResult<CreateBussinessCategory>> Create(CreateBussinessCategory bussinessCategory);
        Task<ServiceResult<List<CreateBussinessCategory>>> CreateList(CreateBussinessCategories bussinessCategories);
        Task<ServiceResult<GetAllBussinessCategory>> GetById(long id);
        Task<ServiceResult<GetAllBussinessCategory>> Update(long id, CreateBussinessCategory bussinessCategory);
        Task<ServiceResult<string>> Delete(long id);
    }
}

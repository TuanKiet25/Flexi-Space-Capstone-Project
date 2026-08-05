using FlexiSpace.Application.ViewModels.Requests.PriorityLevelRQ;
using FlexiSpace.Application.ViewModels.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IPriorityLevelService
    {
        Task<ServiceResult<List<GetAllPriorityLevel>>> GetAll(FilterGetAllPriorityLevel filter);
        Task<ServiceResult<CreatePriorityLevel>> Create(CreatePriorityLevel priorityLevel);
        Task<ServiceResult<GetAllPriorityLevel>> GetById(long id);
        Task<ServiceResult<GetAllPriorityLevel>> Update(long id, CreatePriorityLevel priorityLevel);
        Task<ServiceResult<string>> Delete(long id);
    }
}

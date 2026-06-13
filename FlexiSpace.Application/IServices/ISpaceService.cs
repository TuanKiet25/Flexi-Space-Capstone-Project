using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Application.ViewModels.Responses.Space;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface ISpaceService
    {
        Task<ServiceResult<IEnumerable<GetAllSpace>>> GetAll(FilterGetAllSpace filter);
        Task<ServiceResult<CreateSpaceRP>> Create(CreateSpaceRQ space);
        Task<ServiceResult<GetSpaceByIdRP>> GetById(long id);
        Task<ServiceResult<GetSpaceByIdRP>> Update(long id, CreateSpaceRQ space);
        Task<ServiceResult<string>> Delete(long id);
    }
}

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
        Task<ServiceResult<CreateSpaceRQ>> Create(CreateSpaceRQ space);
    }
}

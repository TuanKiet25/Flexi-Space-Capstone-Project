using FlexiSpace.Application.ViewModels.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IAmenityService
    {
        Task<ServiceResult<IEnumerable<AmenityResponse>>> GetAllAmenitiesBySpaceIdAsync(long spaceId);
    }
}

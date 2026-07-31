using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IAIToolService
    {
        Task<ServiceResult<string>> GenerateImageAsync(GenerateAiImageRequest request);
    }
}

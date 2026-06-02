using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ServiceResult<AuthResponse>> VerifyOtpAsync(VerifyOtpRequest request);
    }
}

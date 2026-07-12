using FlexiSpace.Application.ViewModels.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string email, string otpCode);
        Task ResendOtpEmailAsync(string email, string otpCode);
        Task SendContractOtpEmailAsync(string email, string otpCode, long contractId);
        Task SendContractSuccessEmailAsync(string email, string userName, long contractId);
    }
}

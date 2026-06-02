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
    }
}

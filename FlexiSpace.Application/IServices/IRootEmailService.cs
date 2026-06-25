using FlexiSpace.Application.ViewModels.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IRootEmailService
    {
        Task SendEmailAsync(ResentEmailRequest request);
    }
}

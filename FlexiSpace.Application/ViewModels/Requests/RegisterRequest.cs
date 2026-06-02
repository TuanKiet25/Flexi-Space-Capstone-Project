using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public record RegisterRequest(string Email, string Password,DateTime Dob, string PhoneNumber, string Name, string TurnstileToken);

}

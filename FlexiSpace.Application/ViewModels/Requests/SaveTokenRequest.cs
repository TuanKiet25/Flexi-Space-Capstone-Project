using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class SaveTokenRequest
    {
        public string? Token { get; set; }
        public string? Platform { get; set; }
    }
}

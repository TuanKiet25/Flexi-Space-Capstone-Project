using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class ResentEmailRequest
    {
        public string? ToEmail { get; set; }
        public string? Subject { get; set; }
        public string? HtmlBody { get; set; }
    }
}

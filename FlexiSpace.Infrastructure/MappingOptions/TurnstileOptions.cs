using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.MappingOptions
{
    public class TurnstileOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string VerifyUrl { get; set; } = string.Empty;
        public bool EnableCaptcha { get; set; }
    }
}

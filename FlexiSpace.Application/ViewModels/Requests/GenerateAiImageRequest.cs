using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class GenerateAiImageRequest
    {
        public string Base64Image { get; set; } = null!;
        public string Base64Mask { get; set; } = null!;
        public string Prompt { get; set; } = null!;
    }
}

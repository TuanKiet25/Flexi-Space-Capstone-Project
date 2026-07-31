using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IFalAiService
    {
        Task<string?> GenerateInpaintingAsync(string base64Image, string base64Mask, string prompt);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IExpoPushService
    {
        Task SendPushAsync(List<string> tokens, string title, string body, object data = null!);
    }
}

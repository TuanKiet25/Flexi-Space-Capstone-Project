using FlexiSpace.Application.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class ExpoPushService : IExpoPushService
    {
        private readonly HttpClient _httpClient;

        public ExpoPushService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task SendPushAsync(List<string> tokens, string title, string body, object data = null!)
        {
            if (tokens == null || !tokens.Any()) return;

            var messages = tokens.Select(token => new
            {
                to = token,
                title = title,
                body = body,
                data = data,
                sound = "default"
            });

            var response = await _httpClient.PostAsJsonAsync("https://exp.host/--/api/v2/push/send", messages);
        }
    }
}

using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Infrastructure.MappingOptions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Services
{
    public class TurnstileService : ITurnstileService
    {
        private readonly HttpClient _httpClient;
        private readonly TurnstileOptions _settings;
        public TurnstileService(HttpClient httpClient, IOptions<TurnstileOptions> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }
        private class TurnstileResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
        }
        public async Task<bool> VerifyTokenAsync(string token)
        {
            if (!_settings.EnableCaptcha) return true;
            if (string.IsNullOrEmpty(token)) return false;

            var content = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("secret", _settings.SecretKey),
            new KeyValuePair<string, string>("response", token)
        });

            try
            {
                var response = await _httpClient.PostAsync(_settings.VerifyUrl, content);
                var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>();
                return result?.Success ?? false;
            }
            catch
            {
                return false; 
            }
        }
    }
}

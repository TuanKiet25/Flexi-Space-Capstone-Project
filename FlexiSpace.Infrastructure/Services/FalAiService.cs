using FlexiSpace.Application.IServices;
using FlexiSpace.Infrastructure.MappingOptions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Services
{
    public class FalAiService : IFalAiService
    {
        private readonly HttpClient _httpClient;
        private readonly FalAiOptions _settings;

        // Tiêm IOptions<FalAiSettings> thay vì IConfiguration
        public FalAiService(HttpClient httpClient, IOptions<FalAiOptions> options)
        {
            _httpClient = httpClient;
            _settings = options.Value; 
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Key", _settings.ApiKey);
        }
        public async Task<string?> GenerateInpaintingAsync(string base64Image, string base64Mask, string prompt)
        {
            {
                var payload = new
                {
                    prompt = prompt,
                    image_url = base64Image,
                    mask_url = base64Mask,
                    output_format = "jpeg",
                    num_inference_steps = 25
                };

                var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://fal.run/fal-ai/nano-banana-2/fill", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Fal.ai trả về lỗi {(int)response.StatusCode} " +
                        $"({response.StatusCode})" + error);
                }
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(responseString);

                return doc.RootElement.GetProperty("images")[0].GetProperty("url").GetString();
            }
        }
    }
}

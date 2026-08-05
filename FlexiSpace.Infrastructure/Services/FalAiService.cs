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
        public async Task<string?> GenerateInpaintingAsync(string base64Image, string prompt)
        {
            {
                var payload = new
                {
                    prompt = EnhanceInpaintingPrompt(prompt),
                    image_urls = new[] { base64Image },
                    system_prompt = @"You are an expert interior designer and architectural photo editor. Your absolute priority is strict spatial awareness and boundary control.
                                        CRITICAL RULES:
                                            1. TARGET: Find the solid red shape in the image and replace it with the requested object.
                                            2. AUTO-SCALING (MANDATORY): Analyze the size and proportion of the red shape. You MUST dynamically resize, simplify, or adjust the requested object so it fits STRICTLY INSIDE the red boundaries. Do not generate oversized setups.
                                            3. PERSPECTIVE: Read the depth of the room (floor, walls) and align the object's 3D perspective accordingly. Ensure realistic ground contact and shadows.
                                            4. PRESERVATION: DO NOT touch, modify, or hallucinate any pixels outside the red shape. Keep the original background intact.",
                    output_format = "jpeg",
                    num_inference_steps = 25
                };

                var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://fal.run/fal-ai/nano-banana-2/edit", jsonContent);

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
        private string EnhanceInpaintingPrompt(string userPrompt)
        {
            string basePrompt = userPrompt.Trim();
            return $"Tạo ra đối tượng sau: '{userPrompt}'.\n" +
                   $"CHỈ THỊ BẮT BUỘC: Hãy thiết kế '{userPrompt}' này sao cho kích thước của nó VỪA KHÍT 100% bên trong mảng màu đỏ. Nếu mảng màu đỏ nhỏ, hãy tạo ra một phiên bản '{userPrompt}' nhỏ gọn, tối giản (ví dụ: chỉ 1 màn hình). Tuyệt đối không vẽ tràn ra ngoài ranh giới màu đỏ.";
        }
    }
}

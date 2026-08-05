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
        public async Task<string?> GenerateInpaintingAsync(string base64ImageWithRedMask, string userPrompt, string? base64ObjectImage = null)
        {
            var imageUrls = new List<string> { base64ImageWithRedMask };

            string wrappedPrompt = "";
            string systemPrompt = "";
            if (!string.IsNullOrEmpty(base64ObjectImage))
            {
                imageUrls.Add(base64ObjectImage);
                wrappedPrompt = $"Tôi đã cung cấp 2 bức ảnh.\n" +
                                $"- Ảnh 1: Không gian gốc có chứa một mảng màu đỏ.\n" +
                                $"- Ảnh 2: Hình ảnh vật thể tôi muốn dùng làm mẫu.\n" +
                                $"YÊU CẦU: Hãy trích xuất vật thể trong Ảnh 2, kết hợp với yêu cầu '{userPrompt}' (nếu có), " +
                                $"sau đó ghép vật thể đó vào VỪA KHÍT 100% bên trong mảng màu đỏ ở Ảnh 1. Căn chỉnh góc nhìn và ánh sáng sao cho ăn nhập với không gian Ảnh 1.";

                systemPrompt = @"You are an expert architectural photo editor and compositing AI. You will receive two images.
                                    Image 1: The background environment containing a solid red mask shape.
                                    Image 2: The reference object.
                                    CRITICAL RULES:
                                    1. EXTRACTION: Identify the main object in Image 2.
                                    2. COMPOSITING: Insert that object into Image 1, strictly replacing the solid red shape.
                                    3. AUTO-SCALING: The inserted object MUST be dynamically resized, skewed, and adjusted to fit strictly inside the red boundaries of Image 1.
                                    4. PRESERVATION: DO NOT alter any background elements in Image 1 outside the red shape. Add realistic ground shadows to match Image 1's lighting.";
            }
            else
            {
                wrappedPrompt = $"Tạo ra đối tượng sau: '{userPrompt}'.\nCHỈ THỊ BẮT BUỘC: Hãy thiết kế '{userPrompt}' này sao cho kích thước của nó VỪA KHÍT 100% bên trong mảng màu đỏ. Nếu mảng đỏ nhỏ, hãy tạo ra phiên bản tối giản. Căn chỉnh góc nhìn 3D chạm đúng xuống sàn nhà. Tuyệt đối không vẽ tràn ra ngoài.";

                systemPrompt = @"You are an expert interior designer and architectural photo editor. Your absolute priority is strict spatial awareness and boundary control.
                                CRITICAL RULES:
                                1. TARGET: Replace the solid red shape with the requested object.
                                2. AUTO-SCALING: You MUST dynamically resize or simplify the requested object so it fits STRICTLY INSIDE the red boundaries. Never exceed the red shape.
                                3. PERSPECTIVE: Align the object's 3D perspective with the floor and walls. Add ground shadows.
                                4. PRESERVATION: DO NOT modify any pixels outside the red shape.";
            }

            // TẠO PAYLOAD CHUNG
            var payload = new
            {
                prompt = wrappedPrompt,
                image_urls = imageUrls.ToArray(), 
                system_prompt = systemPrompt,
                output_format = "jpeg",
                num_inference_steps = 30
            };

            var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://fal.run/fal-ai/nano-banana-2/edit", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Fal.ai trả về lỗi {(int)response.StatusCode}: {error}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);

            return doc.RootElement.GetProperty("images")[0].GetProperty("url").GetString();
        }
    }
}

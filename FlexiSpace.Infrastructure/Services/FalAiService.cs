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
                                $"CHỈ THỊ CỐT LÕI: Dựa vào từ khóa '{userPrompt}', hãy tách vật thể ra khỏi Ảnh 2. " +
                                $"Coi mảng màu đỏ trong Ảnh 1 là một HỘP GIỚI HẠN (Bounding Box). Bạn BẮT BUỘC PHẢI THU NHỎ (scale down) vật thể vừa tách ra sao cho nó NẰM LỌT THỎM và VỪA VẶN 100% bên trong hình dáng của mảng đỏ đó. KHÔNG ĐƯỢC VẼ TRÀN RA NGOÀI.";
                systemPrompt = @"You are a strict architectural photo compositing AI. You receive 2 images.
                                Image 1: The background with a solid red mask shape.
                                Image 2: The reference object.
                                MANDATORY RULES:
                                1. TARGET: Extract the main object from Image 2.
                                2. STRICT BOUNDING BOX: The solid red shape in Image 1 is your absolute bounding box. You MUST DOWNSCALE and RESIZE the extracted object so its dimensions fit COMPLETELY INSIDE the red area. ZERO OVERFLOW is allowed beyond the red borders.
                                3. PERSPECTIVE: Maintain the object's original proportions while shrinking it. Anchor its base to the floor angle of Image 1 and add realistic contact shadows.
                                4. BACKGROUND PRESERVATION: Erase the red mask completely but DO NOT alter any other pixels in Image 1.";
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

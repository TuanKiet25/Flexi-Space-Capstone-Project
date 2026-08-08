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
                                $"- Ảnh 1: Không gian gốc có chứa một mảng màu xanh lá neon đánh dấu vị trí.\n" +
                                $"- Ảnh 2: Hình ảnh vật thể tôi muốn dùng làm mẫu.\n" +
                                $"CHỈ THỊ CỐT LÕI (Bắt buộc tuân thủ):\n" +
                                $"1. Tách vật thể ({userPrompt}) ra khỏi Ảnh 2.\n" +
                                $"2. VỊ TRÍ TUYỆT ĐỐI: Xóa mảng màu xanh lá neon ở Ảnh 1 và ĐẶT CHÍNH XÁC vật thể vừa tách vào ĐÚNG VỊ TRÍ đó. TUYỆT ĐỐI KHÔNG đặt vật thể ở bất kỳ khu vực nào khác trong không gian.\n" +
                                $"3. TỶ LỆ (SCALE): Coi hình dáng vùng xanh lá neon là HỘP GIỚI HẠN (Bounding Box). BẮT BUỘC thay đổi kích thước vật thể sao cho nó NẰM LỌT THỎM và VỪA VẶN 100% bên trong tọa độ của mảng xanh lá neon. KHÔNG ĐƯỢC VẼ TRÀN ra ngoài viền xanh lá.\n" +
                                $"4. Giữ nguyên 100% cảnh vật xung quanh, chỉ thay thế đúng mảng màu xanh lá neon.";

                systemPrompt = @"You are a strict architectural photo compositing AI. You receive 2 images.
                    Image 1: The background with a specific solid neon green mask shape indicating the exact target location.
                    Image 2: The reference object.
                    MANDATORY RULES:
                    1. EXTRACTION: Extract the main object from Image 2.
                    2. EXACT POSITIONING: You MUST place the extracted object EXACTLY at the coordinates of the solid neon green shape in Image 1. DO NOT move or place the object anywhere else in the room/environment.
                    3. STRICT BOUNDING BOX: The neon green shape dictates the exact size. DOWNSCALE or RESIZE the extracted object so it fits COMPLETELY INSIDE the original neon green area's boundaries. ZERO OVERFLOW allowed.
                    4. PERSPECTIVE: Anchor the object naturally within that specific neon green location, matching the floor angle and adding contact shadows.
                    5. BACKGROUND PRESERVATION: Erase the neon green mask completely. DO NOT alter, hallucinate, or touch any other pixels in Image 1. The background must remain 100% unchanged.";
            }
            else
            {
                wrappedPrompt = $"Tạo ra đối tượng sau: '{userPrompt}'.\nCHỈ THỊ BẮT BUỘC: Hãy thiết kế '{userPrompt}' này sao cho kích thước của nó VỪA KHÍT 100% bên trong mảng màu xanh lá neon. Nếu mảng xanh lá neon nhỏ, hãy tạo ra phiên bản tối giản. Căn chỉnh góc nhìn 3D chạm đúng xuống sàn nhà. Tuyệt đối không vẽ tràn ra ngoài.";

                systemPrompt = @"You are an expert interior designer and architectural photo editor. Your absolute priority is strict spatial awareness and boundary control.
                    CRITICAL RULES:
                    1. TARGET: Replace the solid neon green shape with the requested object.
                    2. AUTO-SCALING: You MUST dynamically resize or simplify the requested object so it fits STRICTLY INSIDE the neon green boundaries. Never exceed the neon green shape.
                    3. PERSPECTIVE: Align the object's 3D perspective with the floor and walls. Add ground shadows.
                    4. PRESERVATION: DO NOT modify any pixels outside the neon green shape.";
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

using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Infrastructure.MappingOptions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailOptions _mailOptions;
        private readonly IRootEmailService _rootEmailService;
        public EmailService(IOptions<MailOptions> mailOptions, IRootEmailService rootEmailService)
        {
            _mailOptions = mailOptions.Value;
            _rootEmailService = rootEmailService;
        }

        public async Task SendOtpEmailAsync(string email, string otpCode)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_mailOptions.SenderName, _mailOptions.SenderEmail));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "Mã xác thực OTP của bạn";

            var builder = new BodyBuilder
            {
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xác thực tài khoản</h2>
                    <p>Mã OTP của bạn là: 
                        <strong style='font-size: 24px; color: #d9534f;'>{otpCode}</strong>
                    </p>
                    <p>Mã này sẽ hết hạn trong 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                </div>"
            };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_mailOptions.SmtpServer, _mailOptions.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_mailOptions.SenderEmail, _mailOptions.Password);
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể gửi email: {ex.Message}");
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
        public async Task ResendOtpEmailAsync(string email, string otpCode)
        {
            string subject = "Mã xác thực OTP của bạn";
            string htmlTemplate = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xác thực tài khoản</h2>
                    <p>Mã OTP của bạn là: 
                        <strong style='font-size: 24px; color: #d9534f;'>{otpCode}</strong>
                    </p>
                    <p>Mã này sẽ hết hạn trong 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                </div>";
            var emailRequest = new ResentEmailRequest
            {
                ToEmail = email,
                Subject = subject,
                HtmlBody = htmlTemplate
            };


            await _rootEmailService.SendEmailAsync(emailRequest);
        }
        public async Task SendContractOtpEmailAsync(string email, string otpCode, long contractId)
        {
            string subject = $"[FlexiSpace] Mã xác thực ký hợp đồng điện tử {contractId}";

            string htmlMessage = $@"
<div style=""font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f4f7f6; padding: 20px;"">
    
    <!-- Wrapper -->
    <div style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);"">
        
        <!-- Header -->
        <div style=""background-color: #0056b3; padding: 20px; text-align: center;"">
            <h1 style=""color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 1px;"">FlexiSpace</h1>
            <p style=""color: #e2e8f0; margin: 5px 0 0 0; font-size: 14px;"">Nền tảng cho thuê không gian linh hoạt</p>
        </div>

        <!-- Body -->
        <div style=""padding: 30px;"">
            <h2 style=""color: #333333; font-size: 20px; margin-top: 0; text-align: center;"">Xác Nhận Ký Hợp Đồng Điện Tử</h2>
            
            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">
                Xin chào,
            </p>
            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">
                Bạn đang thực hiện thao tác xác nhận đồng ý các điều khoản và tiến hành ký kết hợp đồng trực tuyến (Mã hợp đồng: <strong>{contractId}</strong>) trên hệ thống FlexiSpace.
            </p>
            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">
                Để hoàn tất thủ tục, vui lòng sử dụng mã xác thực (OTP) dưới đây:
            </p>

            <!-- OTP Box -->
            <div style=""text-align: center; margin: 35px 0;"">
                <span style=""display: inline-block; background-color: #f0f7ff; border: 2px dashed #0056b3; color: #0056b3; font-size: 32px; font-weight: bold; letter-spacing: 8px; padding: 15px 40px; border-radius: 6px;"">
                    {otpCode}
                </span>
            </div>

            <p style=""color: #d9534f; font-size: 15px; text-align: center; margin-bottom: 30px; font-weight: bold;"">
                * Mã này sẽ hết hạn trong vòng 5 phút.
            </p>

            <!-- Divider -->
            <hr style=""border: none; border-top: 1px solid #eeeeee; margin: 25px 0;"">

            <!-- Security Warning -->
            <div style=""background-color: #fff9fa; border-left: 4px solid #d9534f; padding: 15px;"">
                <p style=""color: #666666; font-size: 13px; line-height: 1.5; margin: 0;"">
                    <strong>Cảnh báo bảo mật:</strong> Mã OTP này có giá trị pháp lý tương đương với chữ ký của bạn. Tuyệt đối <strong>KHÔNG</strong> chia sẻ mã này cho bất kỳ ai, kể cả nhân viên FlexiSpace. Nếu bạn không thực hiện giao dịch này, vui lòng bỏ qua email hoặc đổi mật khẩu tài khoản ngay lập tức.
                </p>
            </div>
        </div>

        <!-- Footer -->
        <div style=""background-color: #f8f9fa; padding: 20px; text-align: center; border-top: 1px solid #eeeeee;"">
            <p style=""color: #999999; font-size: 12px; margin: 0;"">
                &copy; {DateTime.UtcNow.Year} FlexiSpace. All rights reserved.
            </p>
            <p style=""color: #999999; font-size: 12px; margin: 5px 0 0 0;"">
                Đây là email tự động từ hệ thống. Vui lòng không trả lời email này.
            </p>
        </div>

    </div>
</div>
";

            // Tạo request gửi email
            var emailRequest = new ResentEmailRequest
            {
                ToEmail = email,
                Subject = subject,
                HtmlBody = htmlMessage
            };

            // Gọi hàm gửi mail thực tế
            await _rootEmailService.SendEmailAsync(emailRequest);
        }
        public async Task SendContractSuccessEmailAsync(string email, string userName, long contractId)
        {
            string subject = $"[FlexiSpace] 🎉 Chúc mừng! Hợp đồng {contractId} đã chính thức có hiệu lực";
            //string contractLink = $"https://flexispace.vn/contracts/detail/{contractId}"; // Thay URL thực tế của FE vào đây

            string htmlMessage = $@"
<div style=""font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f4f7f6; padding: 20px;"">
    
    <!-- Wrapper -->
    <div style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);"">
        
        <!-- Header -->
        <div style=""background-color: #0056b3; padding: 20px; text-align: center;"">
            <h1 style=""color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 1px;"">FlexiSpace</h1>
            <p style=""color: #e2e8f0; margin: 5px 0 0 0; font-size: 14px;"">Nền tảng cho thuê không gian linh hoạt</p>
        </div>

        <!-- Body -->
        <div style=""padding: 30px;"">
            <!-- Icon Success (Dùng Unicode) -->
            <div style=""text-align: center; font-size: 48px; margin-bottom: 10px;"">✅</div>
            
            <h2 style=""color: #28a745; font-size: 22px; margin-top: 0; text-align: center;"">Ký Kết Thành Công!</h2>
            
            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">
                Xin chào <strong>{userName}</strong>,
            </p>
            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">
                FlexiSpace xin chúc mừng bạn! Hợp đồng thuê không gian mang mã số <strong style=""color: #0056b3;"">{contractId}</strong> đã được cả hai bên (Chủ mặt bằng và Người thuê) hoàn tất việc xác thực chữ ký điện tử.
            </p>
            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">
                Hợp đồng hiện tại <strong>đã chính thức có hiệu lực pháp lý</strong>. Toàn bộ thông tin định danh (CCCD), thời gian ký và địa chỉ IP của hai bên đã được hệ thống lưu vết bảo mật.
            </p>

            <hr style=""border: none; border-top: 1px solid #eeeeee; margin: 25px 0;"">

            <!-- Next Steps -->
            <div style=""background-color: #f8f9fa; border-left: 4px solid #0056b3; padding: 15px;"">
                <p style=""color: #333333; font-size: 14px; margin: 0 0 10px 0; font-weight: bold;"">Bước tiếp theo cần làm:</p>
                <ul style=""color: #666666; font-size: 13px; line-height: 1.5; margin: 0; padding-left: 20px;"">
                    <li>Người thuê vui lòng tiến hành thanh toán tiền cọc (nếu có) theo quy định trong hợp đồng.</li>
                    <li>Cả hai bên có thể tải xuống bản PDF của hợp đồng bất kỳ lúc nào tại hệ thống.</li>
                </ul>
            </div>
        </div>

        <!-- Footer -->
        <div style=""background-color: #f8f9fa; padding: 20px; text-align: center; border-top: 1px solid #eeeeee;"">
            <p style=""color: #999999; font-size: 12px; margin: 0;"">
                &copy; {DateTime.UtcNow.Year} FlexiSpace. All rights reserved.
            </p>
            <p style=""color: #999999; font-size: 12px; margin: 5px 0 0 0;"">
                Đây là email tự động từ hệ thống. Vui lòng không trả lời email này.
            </p>
        </div>

    </div>
</div>
";

            var emailRequest = new ResentEmailRequest
            {
                ToEmail = email,
                Subject = subject,
                HtmlBody = htmlMessage
            };

            await _rootEmailService.SendEmailAsync(emailRequest);
        }
    }

}


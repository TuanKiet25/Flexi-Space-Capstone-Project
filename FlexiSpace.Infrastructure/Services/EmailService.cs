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
            // 1. Tạo giao diện HTML cho Email (Có thể trang trí CSS tùy thích)
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

    }

}


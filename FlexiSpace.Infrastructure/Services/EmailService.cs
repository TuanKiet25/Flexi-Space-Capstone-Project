using FlexiSpace.Application.IServices;
using FlexiSpace.Infrastructure.MappingOptions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailOptions _mailOptions;

        public EmailService(IOptions<MailOptions> mailOptions)
        {
            _mailOptions = mailOptions.Value;
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
    }
}

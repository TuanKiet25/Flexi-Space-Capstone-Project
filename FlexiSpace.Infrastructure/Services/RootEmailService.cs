using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Infrastructure.MappingOptions;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Services
{
    public class RootEmailService : IRootEmailService
    {
        private readonly ResentEmailOptions _resentEmailOptions;
        private readonly IHttpClientFactory _httpClientFactory;

        public RootEmailService(IOptions<ResentEmailOptions> resentMailOptions, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _resentEmailOptions = resentMailOptions.Value;
        }

       
        public async Task SendEmailAsync(ResentEmailRequest request)
        {
                var apiKey = _resentEmailOptions.ApiKey;
                var fromEmail = _resentEmailOptions.FromEmail;
                var fromName = _resentEmailOptions.FromName;
                var emailPayload = new
                {
                    from = $"{fromName} <{fromEmail}>",
                    to = new[] { request.ToEmail },
                    subject = request.Subject,
                    html = request.HtmlBody
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(emailPayload),
                    Encoding.UTF8,
                    "application/json"
                );

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var response = await client.PostAsync("https://api.resend.com/emails", jsonContent);

                response.EnsureSuccessStatusCode();
            }
        }
    }


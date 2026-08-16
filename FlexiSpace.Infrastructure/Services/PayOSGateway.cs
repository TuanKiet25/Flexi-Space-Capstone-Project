using FlexiSpace.Application.IServices;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace FlexiSpace.Infrastructure.Services
{
    public class PayOSGateway : IPayOSGateway
    {
        private readonly PayOSClient _payOS;

        public PayOSGateway(PayOSClient payOS)
        {
            _payOS = payOS;
        }

        public async Task<string> CreatePaymentLinkAsync(int orderCode, int amount, string description, string returnUrl, string cancelUrl)
        {
            var paymentData = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = description,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl
            };

            var paymentLinkResponse = await _payOS.PaymentRequests.CreateAsync(paymentData);
            return paymentLinkResponse.CheckoutUrl;
        }

        public async Task<long> VerifyWebhookOrderCodeAsync(Webhook webhookData)
        {
            var verifiedData = await _payOS.Webhooks.VerifyAsync(webhookData);
            return verifiedData.OrderCode;
        }
    }
}

using PayOS.Models.Webhooks;

namespace FlexiSpace.Application.IServices
{
    public interface IPayOSGateway
    {
        Task<string> CreatePaymentLinkAsync(int orderCode, int amount, string description, string returnUrl, string cancelUrl);
        Task<long> VerifyWebhookOrderCodeAsync(Webhook webhookData);
    }
}

using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface ITransactionService
    {
        public Task<ServiceResult<string>> CreateTransactionAsync(TransactionRequest request);
        public Task<bool> VerifyWebhookSuccess(Webhook webhookData);
    }
}

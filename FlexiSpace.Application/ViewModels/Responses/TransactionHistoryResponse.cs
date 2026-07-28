using FlexiSpace.Domain.Enum;
using System;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class TransactionHistoryResponse : BaseVModel
    {
        public long Id { get; set; }
        public long WalletId { get; set; }
        public decimal WalletAmount { get; set; }
        public decimal TransactionAmount { get; set; }
        public TransactionEnum Status { get; set; }
        public string Description { get; set; }
    }
}

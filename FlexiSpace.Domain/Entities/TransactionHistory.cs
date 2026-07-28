using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class TransactionHistory : BaseEntity
    {
        public long Id { get; set; }
        public long? WalletId { get; set; }
        public decimal WalletAmount { get; set; }
        public decimal TransactionAmount { get; set; }
        public TransactionEnum Status { get; set; }
        public string Description { get; set; }

        public virtual Wallet Wallet { get; set; }
    }
}

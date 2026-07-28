using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Wallet : BaseEntity
    {
        public Wallet()
        {
            Transactions = new HashSet<Transaction>();
            TransactionHistories = new HashSet<TransactionHistory>();
        }

        public long Id { get; set; }
        public string UserId { get; set; }
        public decimal Balance { get; set; }
        public virtual User User { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
        public virtual ICollection<TransactionHistory> TransactionHistories { get; set; }
    }
}

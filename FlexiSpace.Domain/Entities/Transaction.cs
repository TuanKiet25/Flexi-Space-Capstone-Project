using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Transaction : BaseEntity
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public virtual User User { get; set; }
        
    }
}

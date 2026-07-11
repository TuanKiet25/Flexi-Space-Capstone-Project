using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class ContractVerification
    {
        public long Id { get; set; }
        public long ContractId { get; set; }
        public string CardNumber { get; set; }
        public string ScannedName { get; set; }
        public bool IsMatched { get; set; }
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
        public string IPAddress { get; set; }
        public string RawQRData { get; set; }
        public virtual Contract Contract { get; set; }
    }
}

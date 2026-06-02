using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class UserOTP
    {
        public string Id { get; set; } = Ulid.NewUlid().ToString();
        public string UserId { get; set; } 
        public string Email { get; set; }    
        public string OtpCode { get; set; } 
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; } = false;
        public virtual User User { get; set; }
    }
}

using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class UserProfile : BaseEntity
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public string Bio { get; set; }
        public string SocialLink { get; set; }
        public Gender Gender { get; set; }
        // Chỉ số uy tín 
        //public bool IsIdentityVerified { get; set; } = false;
        //public int ReputationScore { get; set; } = 100;
        //public int SuccessfulDeals { get; set; } = 0;
        public virtual User User { get; set; }
    }
}

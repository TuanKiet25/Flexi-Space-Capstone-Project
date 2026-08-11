using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class SpaceUsageRight : BaseEntity
    {
        public long Id { get; set; }

        public long SpaceId { get; set; }
        public long? ContractId { get; set; }

        public string UserId { get; set; }      
        public string GrantedByUserId { get; set; } 

        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }

        public bool CanShare { get; set; }
        public bool CanGrantSharePermission { get; set; }

        public SpaceUsageRightType Type { get; set; }

        public virtual Space Space { get; set; }
        public virtual Contract? Contract { get; set; }
        public virtual User User { get; set; }
        public virtual User GrantedByUser { get; set; }
    }
}

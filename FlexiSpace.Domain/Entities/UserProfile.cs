using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class UserProfile : BaseEntity
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string AvartarUrl { get; set; }
        public virtual User User { get; set; }
    }
}

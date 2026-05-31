using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public partial class User : BaseEntity
    {
        public User() 
        {
            Notification = new HashSet<Notification>();
        }

        public string UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public RoleEnum Role { get; set; }

        public virtual Profile Profile { get; set; }
        public virtual ICollection<Notification> Notification { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class UserAiImageHistory
    {
        public long Id { get; set; }

        public string UserId { get; set; } = null!; 
        public string Prompt { get; set; } = null!;
        public string ResultImageUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

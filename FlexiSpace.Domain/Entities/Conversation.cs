    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class Conversation
    {
        public string Id { get; set; } = Ulid.NewUlid().ToString();
        public string LessorId { get; set; }
        public string LesseeId { get; set; }
        public virtual User Lessor { get; set; }
        public virtual User Lessee { get; set; }
        public DateTime LastMessage { get; set; }
        public virtual ICollection<Contract> Contracts { get; set; }
        public virtual ICollection<Message> Messages { get; set; }

    }
}

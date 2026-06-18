using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests.Contract
{
    public class FilterGetAllContract
    {
        public string? LessorId { get; set; }
        public string? LesseeId { get; set; }
        public long? SpaceId { get; set; }
        public int? Status { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ShareSpaceCategoryResponse
    {
        public long Id { get; set; }
        public long BussinessCategoryId { get; set; }
        public long ShareSpaceDetailId { get; set; }
        public string? Note { get; set; }
    }
}

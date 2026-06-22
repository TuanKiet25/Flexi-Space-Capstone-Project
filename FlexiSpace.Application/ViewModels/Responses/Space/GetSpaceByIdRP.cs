using FlexiSpace.Application.ViewModels.Requests.Space;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses.Space
{
    public class GetSpaceByIdRP : CreateSpaceRQ
    {
        public new long Id { get; set; }
        public List<PictureURLVModel> PictureURLs { get; set; } = new List<PictureURLVModel>();
    }
}

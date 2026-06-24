using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests.BussinessCategoryRQ
{
    public class CreateBussinessCategories
    {
        public List<CreateBussinessCategory> Categories { get; set; } = new List<CreateBussinessCategory>();
    }
}

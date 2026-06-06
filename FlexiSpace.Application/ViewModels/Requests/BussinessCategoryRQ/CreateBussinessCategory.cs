using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests.BussinessCategoryRQ
{
    public class CreateBussinessCategory
    {
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        [JsonIgnore]
        public string? CreatedBy { get; set; }
    }

    public class GetAllBussinessCategory : BaseVModel
    {
    }

    public class FilterGetAllBussinessCategory : BaseVModel
    {
    }
}
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class WalletRespnse : BaseVModel
    {
        public long Id { get; set; }
        public decimal Balance { get; set; }
        public UserResponse? User { get; set; }
    }
}

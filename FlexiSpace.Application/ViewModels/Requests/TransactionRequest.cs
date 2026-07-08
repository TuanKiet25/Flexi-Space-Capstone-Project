using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class TransactionRequest
    {
        [Required]
        public decimal Amount { get; set; }

        public string? ReturnUrl { get; set; }

        public string? CancelUrl { get; set; }
    }
}

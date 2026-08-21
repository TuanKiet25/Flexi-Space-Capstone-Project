using FlexiSpace.Application.ViewModels.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface ITransactionHistoryService
    {
        Task<ServiceResult<IEnumerable<TransactionHistoryResponse>>> GetAllTransactionHistoryByUserId();
        Task<ServiceResult<IEnumerable<TransactionHistoryResponse>>> GetTransactionHistoryByUserIdAsync(string userId);
    }
}

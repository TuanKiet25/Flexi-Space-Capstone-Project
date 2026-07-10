using FlexiSpace.Application.ViewModels.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IWalletService
    {
        public Task<ServiceResult<IEnumerable<WalletRespnse>>> GetAllWallet();
        public Task<ServiceResult<WalletRespnse>> GetWalletByUserId(string userId);
        public Task<ServiceResult<WalletRespnse>> GetOwnWallet();
        public Task<ServiceResult<WalletRespnse>> UpdateWalletBalance(string userId, decimal uBalance);
        public Task<ServiceResult<WalletRespnse>> SpendWalletBalance(decimal spend);
    }
}

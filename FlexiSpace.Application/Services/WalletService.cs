using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public WalletService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<IEnumerable<WalletRespnse>>> GetAllWallet()
        {
            try
            {
                var wallets = await _unitOfWork.walletRepository.GetAllAsync(
                    filter: x => !x.IsDeleted,
                    include: q => q.Include(w => w.User).ThenInclude(u => u.Profile)
                );

                var mapped = _mapper.Map<IEnumerable<WalletRespnse>>(wallets);

                return new ServiceResult<IEnumerable<WalletRespnse>>
                {
                    IsSuccess = true,
                    Data = mapped
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<WalletRespnse>>
                {
                    IsSuccess = false,
                    Message = $"Error retrieving all wallets: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<WalletRespnse>> GetOwnWallet()
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return new ServiceResult<WalletRespnse>
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                var wallet = await _unitOfWork.walletRepository.GetAsync(
                    filter: x => x.UserId == userId && !x.IsDeleted,
                    include: q => q.Include(w => w.User).ThenInclude(u => u.Profile)
                );

                if (wallet == null)
                {
                    return new ServiceResult<WalletRespnse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Wallet not found."
                    };
                }

                var mapped = _mapper.Map<WalletRespnse>(wallet);

                return new ServiceResult<WalletRespnse>
                {
                    IsSuccess = true,
                    Data = mapped
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<WalletRespnse>
                {
                    IsSuccess = false,
                    Message = $"Error retrieving user's wallet: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<WalletRespnse>> GetWalletByUserId(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return new ServiceResult<WalletRespnse>
                    {
                        IsSuccess = false,
                        Message = "UserId is required."
                    };
                }

                var wallet = await _unitOfWork.walletRepository.GetAsync(
                    filter: x => x.UserId == userId && !x.IsDeleted,
                    include: q => q.Include(w => w.User).ThenInclude(u => u.Profile)
                );

                if (wallet == null)
                {
                    return new ServiceResult<WalletRespnse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Wallet not found."
                    };
                }

                var mapped = _mapper.Map<WalletRespnse>(wallet);

                return new ServiceResult<WalletRespnse>
                {
                    IsSuccess = true,
                    Data = mapped
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<WalletRespnse>
                {
                    IsSuccess = false,
                    Message = $"Error retrieving wallet by UserId: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<WalletRespnse>> SpendWalletBalance(decimal spend)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return new ServiceResult<WalletRespnse>
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                var wallet = await _unitOfWork.walletRepository.GetAsync(
                    filter: x => x.UserId == userId && !x.IsDeleted,
                    include: q => q.Include(w => w.User).ThenInclude(u => u.Profile)
                );

                if (wallet == null)
                {
                    return new ServiceResult<WalletRespnse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Wallet not found."
                    };
                }

                decimal amountToSubtract = Math.Abs(spend);
                if (wallet.Balance < amountToSubtract)
                {
                    return new ServiceResult<WalletRespnse>
                    {
                        IsSuccess = false,
                        Message = "Balance not enough"
                    };
                }

                wallet.Balance -= amountToSubtract;
                wallet.UpdatedAt = DateTime.UtcNow;
                wallet.UpdatedBy = userId;

                await _unitOfWork.walletRepository.UpdateAsync(wallet);
                await _unitOfWork.SaveChangesAsync();

                var mapped = _mapper.Map<WalletRespnse>(wallet);

                return new ServiceResult<WalletRespnse>
                {
                    IsSuccess = true,
                    Data = mapped
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<WalletRespnse>
                {
                    IsSuccess = false,
                    Message = $"Error processing spend transaction: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<WalletRespnse>> UpdateWalletBalance(string userId, decimal uBalance)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return new ServiceResult<WalletRespnse>
                    {
                        IsSuccess = false,
                        Message = "UserId is required."
                    };
                }

                var wallet = await _unitOfWork.walletRepository.GetAsync(
                    filter: x => x.UserId == userId && !x.IsDeleted,
                    include: q => q.Include(w => w.User).ThenInclude(u => u.Profile)
                );

                if (wallet == null)
                {
                    return new ServiceResult<WalletRespnse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Wallet not found."
                    };
                }

                if (wallet.Balance + uBalance < 0)
                {
                    wallet.Balance = 0;
                }
                else
                {
                    wallet.Balance += uBalance;
                }
                
                wallet.UpdatedAt = DateTime.UtcNow;
                wallet.UpdatedBy = _currentUserService.UserId ?? "System";

                await _unitOfWork.walletRepository.UpdateAsync(wallet);
                await _unitOfWork.SaveChangesAsync();

                var mapped = _mapper.Map<WalletRespnse>(wallet);

                return new ServiceResult<WalletRespnse>
                {
                    IsSuccess = true,
                    Data = mapped
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<WalletRespnse>
                {
                    IsSuccess = false,
                    Message = $"Error updating wallet balance: {ex.Message}"
                };
            }
        }
    }
}

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
    public class TransactionHistoryService : ITransactionHistoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public TransactionHistoryService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<IEnumerable<TransactionHistoryResponse>>> GetAllTransactionHistoryByUserId()
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return new ServiceResult<IEnumerable<TransactionHistoryResponse>>
                    {
                        IsSuccess = false,
                        Message = "User is not authenticated."
                    };
                }

                var histories = await _unitOfWork.transactionHistoryRepository.GetAllAsync(
                    filter: x => x.Wallet.UserId == userId && !x.IsDeleted,
                    include: q => q.Include(x => x.Wallet)
                );

                var mapped = _mapper.Map<IEnumerable<TransactionHistoryResponse>>(histories);

                return new ServiceResult<IEnumerable<TransactionHistoryResponse>>
                {
                    IsSuccess = true,
                    Data = mapped
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<TransactionHistoryResponse>>
                {
                    IsSuccess = false,
                    Message = $"Error retrieving transaction history: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<IEnumerable<TransactionHistoryResponse>>> GetTransactionHistoryByUserIdAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return new ServiceResult<IEnumerable<TransactionHistoryResponse>>
                    {
                        IsSuccess = false,
                        Message = "UserId is required."
                    };
                }

                var histories = await _unitOfWork.transactionHistoryRepository.GetAllAsync(
                    filter: x => x.Wallet.UserId == userId && !x.IsDeleted,
                    include: q => q.Include(x => x.Wallet)
                );

                var mapped = _mapper.Map<IEnumerable<TransactionHistoryResponse>>(histories);

                return new ServiceResult<IEnumerable<TransactionHistoryResponse>>
                {
                    IsSuccess = true,
                    Data = mapped
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<TransactionHistoryResponse>>
                {
                    IsSuccess = false,
                    Message = $"Error retrieving transaction history: {ex.Message}"
                };
            }
        }
    }
}

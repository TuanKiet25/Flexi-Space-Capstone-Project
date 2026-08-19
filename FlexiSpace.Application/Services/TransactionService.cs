using CloudinaryDotNet;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.AspNetCore.Http;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Transaction = FlexiSpace.Domain.Entities.Transaction;

namespace FlexiSpace.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayOSGateway _payOSGateway;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TransactionService(IUnitOfWork unitOfWork, IPayOSGateway payOSGateway, IHttpContextAccessor httpContextAccessor   )
        {
            _unitOfWork = unitOfWork;
            _payOSGateway = payOSGateway;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ServiceResult<string>> CreateTransactionAsync(TransactionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CancelUrl) || string.IsNullOrEmpty(request.ReturnUrl))
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "ReturnUrl and CancelUrl are required."
                    };
                }

                if (request.Amount <= 0)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "Amount must be greater than zero."
                    };
                }

                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    throw new Exception("Invalid user ID from token");
                }

                var wallet = await _unitOfWork.walletRepository.GetAsync(x => x.UserId == userIdString);

                var orderCode = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % int.MaxValue);
                var transaction = new Transaction
                {
                    UserId = userIdString,
                    WalletId = wallet?.Id,
                    Amount = request.Amount,
                    TransactionCode = orderCode,
                    Status = TransactionEnum.Pending,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userIdString,
                    IsDeleted = false,
                };

                var checkoutUrl = await _payOSGateway.CreatePaymentLinkAsync(
                    orderCode: orderCode,
                    amount: (int)(request.Amount),
                    description: $"Top up wallet: {request.Amount}",
                    returnUrl: request.ReturnUrl,
                    cancelUrl: request.CancelUrl
                );

                await _unitOfWork.transactionRepository.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<string>
                {
                    IsSuccess = true,
                    Data = checkoutUrl
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<string>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<bool> VerifyWebhookSuccess(Webhook webhookData)
        {
            try
            {
                long payOSOrderCode = await _payOSGateway.VerifyWebhookOrderCodeAsync(webhookData);

                if (payOSOrderCode == 123)
                {
                    return true;
                }

                var transaction = await _unitOfWork.transactionRepository
                    .GetAsync(x => x.TransactionCode == payOSOrderCode);

                if (transaction == null)
                {
                    throw new Exception($"Không tìm thấy giao dịch với mã: {payOSOrderCode}");
                }
                transaction.Status = TransactionEnum.Completed;
                transaction.UpdatedAt = DateTime.UtcNow;
                var userId = transaction.UserId;
                await _unitOfWork.transactionRepository.UpdateAsync(transaction);

                var wallet = await _unitOfWork.walletRepository.GetAsync(x => x.UserId == userId);
                if(wallet == null)
                {
                    var nWallet = new Wallet
                    {
                        UserId = userId,
                        Balance = transaction.Amount,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        IsDeleted = false,
                        IsActive = true
                    };
                    await _unitOfWork.walletRepository.AddAsync(nWallet);
                    transaction.Wallet = nWallet;
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    wallet.Balance += transaction.Amount;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    wallet.UpdatedBy = userId;
                    await _unitOfWork.walletRepository.UpdateAsync(wallet);
                    transaction.WalletId = wallet.Id;
                }

                var transactionHistory = new TransactionHistory
                {
                    WalletId = transaction.WalletId,
                    WalletAmount = (wallet == null || wallet.Balance == 0) ? 0 : wallet.Balance,
                    TransactionAmount = transaction.Amount,
                    Status = TransactionEnum.Completed,
                    Description = $"Top up wallet: {transaction.Amount}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    IsDeleted = false
                };

                await _unitOfWork.transactionRepository.UpdateAsync(transaction);
                await _unitOfWork.transactionHistoryRepository.AddAsync(transactionHistory);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error handling webhook: {ex.Message}", ex);
            }
        }

    }
}

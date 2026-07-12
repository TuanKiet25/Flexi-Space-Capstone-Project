using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Requests.Contract;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.VisualBasic;
using System.Security.Cryptography.X509Certificates;

namespace FlexiSpace.Application.Services
{
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDistributedCache _cache;
        private readonly IEmailService _emailService;
        public ContractService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IDistributedCache cache, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _cache = cache;
            _emailService = emailService;
        }

        public async Task<ServiceResult<ContractResponse>> CreateContractAsync(ContractRequest request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var currentUserId = _currentUserService.UserId;
                var validation = await ValidateRequestAsync(request);
                if (validation.Space!.OwnerId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Lỗi bảo mật: Chỉ chủ sở hữu mặt bằng mới có quyền tạo hợp đồng!"
                    };
                }
                if (validation.ErrorMessage != null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = validation.ErrorMessage
                    };
                }
    
                var contract = _mapper.Map<Contract>(request);
                contract.LessorId = validation.Space!.OwnerId;
                contract.LesseeId = validation.Booking!.LesseeId;
                contract.EndDate = CalculateEndDate(contract.StartDate, contract.DurationUnit, contract.Duration);
                contract.CreatedAt = DateTime.Now;
                contract.UpdatedAt = DateTime.Now;
                contract.ContractVerification = new ContractVerification
                {
                    IsLessorAgreed = false,
                    IsLesseeAgreed = false,
                };
                await _unitOfWork.contractRepository.AddAsync(contract);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ContractResponse>(contract)
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        private DateTime CalculateEndDate(DateTime startDate, DurationUnitEnum durationUnit, int duration)
        {
            return durationUnit switch
            {
                DurationUnitEnum.Weeks => startDate.AddDays(duration * 7),
                DurationUnitEnum.Days=> startDate.AddDays(duration),
                DurationUnitEnum.Months => startDate.AddMonths(duration),
                DurationUnitEnum.Years => startDate.AddYears(duration),
                _ => startDate 
            };
        }

        public async Task<ServiceResult<MessageResponse>> ShareContractAsync(long contractId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var currentUserId = _currentUserService.UserId;
                var contract = await _unitOfWork.contractRepository.GetAsync(x => x.Id == contractId && !x.IsDeleted);

                if (contract == null)
                {
                    return new ServiceResult<MessageResponse>
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy hợp đồng."
                    };
                }

                if (contract.LessorId != currentUserId)
                {
                    return new ServiceResult<MessageResponse>
                    {
                        IsSuccess = false,
                        Message = "Bạn không có quyền chia sẻ hợp đồng này!"
                    };
                }
                if (contract.Status != ContractStatusEnum.Draft)
                {
                    return new ServiceResult<MessageResponse>
                    {
                        IsSuccess = false,
                        Message = "Chỉ có thể chia sẻ hợp đồng ở trạng thái Nháp (Draft)."
                    };
                }

                var proposalMessage = new Message
                {
                    ConversationId = contract.ConversationId,
                    SenderId = currentUserId,
                    Content = contract.Id.ToString(),
                    MessageType = MessageTypeEnum.ContractProposal,
                    CreateAt = DateTime.UtcNow,
                };
                await _unitOfWork.messageRepository.AddAsync(proposalMessage);
                var conversation = await _unitOfWork.conversationRepository.GetAsync(x => x.Id == contract.ConversationId);
                if (conversation != null)
                {
                    conversation.LastMessage = DateTime.UtcNow;
                    await _unitOfWork.conversationRepository.UpdateAsync(conversation);
                }
                
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<MessageResponse>
                {
                    IsSuccess = true,
                    Message = "Đã chia sẻ hợp đồng vào phòng chat.",
                    Data = _mapper.Map<MessageResponse>(proposalMessage)
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<MessageResponse>
                {
                    IsSuccess = false,
                    Message = $"Đã xảy ra lỗi hệ thống: {ex.Message}"
                };
            }
        }
        public async Task<ServiceResult<List<ContractResponse>>> GetAllContractsAsync(FilterGetAllContract filter)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;

                var lessorId = filter?.LessorId;
                var lesseeId = filter?.LesseeId;
                var spaceId = filter?.SpaceId;
                var status = filter?.Status;

                var contracts = await _unitOfWork.contractRepository.GetAllAsync(
                    x => !x.IsDeleted &&
                         (x.LessorId == currentUserId || x.LesseeId == currentUserId) &&
                         (string.IsNullOrWhiteSpace(lessorId) || x.LessorId == lessorId) &&
                         (string.IsNullOrWhiteSpace(lesseeId) || x.LesseeId == lesseeId) &&
                         (spaceId == null || x.SpaceId == spaceId) &&
                         (status == null || x.Status == status),
                    include: q => q.Include(c => c.Space).Include(c => c.PrimaryBookingRequest));

                return new ServiceResult<List<ContractResponse>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<ContractResponse>>(contracts)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<ContractResponse>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<ServiceResult<ContractResponse>> GetContractByIdAsync(long id)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var contract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(c => c.Space).Include(c => c.PrimaryBookingRequest));

                if (contract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy hợp đồng với Id đã cho."
                    };
                }

                if (contract.LessorId != currentUserId && contract.LesseeId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Lỗi bảo mật: Bạn không có quyền truy cập vào hợp đồng này!"
                    };
                }

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ContractResponse>(contract)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ContractResponse>> UpdateContractAsync(long id, ContractRequest request)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;

                var existingContract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(c => c.Space).Include(c => c.PrimaryBookingRequest));

                if (existingContract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy hợp đồng với Id đã cho."
                    };
                }
                if (existingContract.LessorId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Lỗi bảo mật: Bạn không có quyền cập nhật hợp đồng này!"
                    };
                }

                var validation = await ValidateRequestAsync(request);
                if (validation.ErrorMessage != null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = validation.ErrorMessage
                    };
                }
                if (validation.Space!.OwnerId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Lỗi bảo mật: Mặt bằng mới không thuộc sở hữu của bạn!"
                    };
                }

                _mapper.Map(request, existingContract);
                existingContract.EndDate = CalculateEndDate(existingContract.StartDate, existingContract.DurationUnit, existingContract.Duration);
                existingContract.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.contractRepository.UpdateAsync(existingContract);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ContractResponse>(existingContract)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ContractResponse>> DeleteContractAsync(long id)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var existingContract = await _unitOfWork.contractRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if(existingContract.Status != ContractStatusEnum.Draft)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Chỉ có thể xóa hợp đồng ở trạng thái Nháp (Draft)."
                    };
                }
                if (existingContract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy hợp đồng với Id đã cho."
                    };
                }
                if (existingContract.LessorId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Lỗi bảo mật: Chỉ chủ sở hữu mới có quyền xóa hợp đồng!"
                    };
                }

                await _unitOfWork.contractRepository.RemoveByIdAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Message = "Xóa hợp đồng thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<bool>> SendContractOtpAsync(long contractId)
        {
            var currentUserId = _currentUserService.UserId;
            var contract = await _unitOfWork.contractRepository.GetAsync(c => c.Id == contractId);
            if (contract == null) return new ServiceResult<bool> { IsSuccess = false, Message = "Hợp đồng không tồn tại." };
            if (contract.LessorId != currentUserId && contract.LesseeId != currentUserId)
                return new ServiceResult<bool> { IsSuccess = false, Message = "Bạn không có quyền ký hợp đồng này." };
            var userProfile = await _unitOfWork.profileRepository.GetAsync(x => x.UserId == currentUserId);
            if (userProfile == null || !userProfile.IsVerified)
                return new ServiceResult<bool> { IsSuccess = false, Message = "Vui lòng xác thực CCCD trước khi ký hợp đồng." };
            var contractVerification = await _unitOfWork.contractVerificationRepository.GetAsync(v => v.ContractId == contractId, include : q => q.Include(v => v.Contract!));
            bool alreadySigned = (currentUserId == contractVerification.Contract!.LessorId && contractVerification.IsLessorAgreed) ||
                                 (currentUserId == contractVerification.Contract.LesseeId && contractVerification.IsLesseeAgreed);
            if (alreadySigned)
                return new ServiceResult<bool> { IsSuccess = false, Message = "Bạn đã ký hợp đồng này rồi." };
            string otpCode = new Random().Next(100000, 999999).ToString();
            // 2. Tạo Redis Key mang đầy đủ ngữ cảnh
            string redisKey = $"OTP:SignContract:{contractId}:{currentUserId}";
            // 3. Cấu hình thời gian sống (TTL) là 5 phút
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            // 4. Lưu vào Redis
            await _cache.SetStringAsync(redisKey, otpCode, cacheOptions);
            await _emailService.SendContractOtpEmailAsync((await _unitOfWork.userRepository.GetAsync(u => u.UserId == currentUserId)).Email, otpCode, contractId);

            return new ServiceResult<bool> { IsSuccess = true,Data = true, Message = "Mã OTP đã được gửi đến email của bạn." };
        }
        public async Task<ServiceResult<MessageResponse>> ContractValidateOtpAsync(long contractId, string inputOtp)
        {
            Message? returnMessage = null;
            var currentUserId = _currentUserService.UserId;
            bool isFullySigned = false;
            // 1. Tái tạo lại chính xác Key đã dùng khi lưu
            string redisKey = $"OTP:SignContract:{contractId}:{currentUserId}";

            // 2. Lấy mã OTP từ RAM về
            string? savedOtp = await _cache.GetStringAsync(redisKey);

            // 3. Kiểm tra tính hợp lệ của OTP
            if (string.IsNullOrEmpty(savedOtp))
            {
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Mã OTP không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu gửi lại mã OTP." };
            }
            if (savedOtp != inputOtp)
            {
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Mã OTP không hợp lệ." };
            }

            // Xóa OTP ngay lập tức để chống Replay Attack
            await _cache.RemoveAsync(redisKey);

            // 4. Lấy Dữ liệu User & Contract (Gộp query để tối ưu hiệu suất)
            var currentUser = await _unitOfWork.userRepository.GetAsync(
                u => u.UserId == currentUserId,
                include: q => q.Include(u => u.Profile)
            );

            // Lấy Hợp đồng kèm luôn Verification trong 1 câu SQL duy nhất
            var contract = await _unitOfWork.contractRepository.GetAsync(
                c => c.Id == contractId,
                include: q => q.Include(c => c.ContractVerification)
            );
            if (contract == null || contract.ContractVerification == null)
            {
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Hợp đồng không tồn tại hoặc dữ liệu bị lỗi." };
            }
            if (currentUser?.Profile == null)
            {
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Vui lòng cập nhật hồ sơ CCCD trước khi ký hợp đồng." };
            }

            if (currentUserId == contract.LessorId)
            {
                contract.ContractVerification.IsLessorAgreed = true;
                contract.ContractVerification.LessorSignedAt = DateTime.UtcNow;
                contract.ContractVerification.LessorIpAddress = _currentUserService.GetClientIpAddress();
                contract.ContractVerification.LessorSignatureData = "Verified via Email OTP";

                contract.LessorNumberCard = currentUser.Profile.IdentityCardNumber;
                contract.LessorCardAddress = currentUser.Profile.PermanentResidence;
                contract.LessorName = currentUser.Profile.FullName;
                contract.LessorCardIssuanceDate = currentUser.Profile.DateOfIssue;
            }
            else if (currentUserId == contract.LesseeId)
            {
                contract.ContractVerification.IsLesseeAgreed = true;
                contract.ContractVerification.LesseeSignedAt = DateTime.UtcNow;
                contract.ContractVerification.LesseeIpAddress = _currentUserService.GetClientIpAddress();
                contract.ContractVerification.LesseeSignatureData = "Verified via Email OTP";

                contract.LesseeNumberCard = currentUser.Profile.IdentityCardNumber;
                contract.LesseeCardAddress = currentUser.Profile.PermanentResidence;
                contract.LesseeName = currentUser.Profile.FullName;
                contract.LesseeCardIssuanceDate = currentUser.Profile.DateOfIssue;
            }
            else
            {
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Bạn không có quyền ký hợp đồng này." };
            }
            if (contract.ContractVerification.IsLessorAgreed && contract.ContractVerification.IsLesseeAgreed)
            {
                contract.Status = ContractStatusEnum.Active;
                isFullySigned = true;
            }
            if (isFullySigned)
            {
                var otherUserId = currentUserId == contract.LessorId ? contract.LesseeId : contract.LessorId;
                var otherUser = await _unitOfWork.userRepository.GetAsync(u => u.UserId == otherUserId, include: q => q.Include(u => u.Profile));

                _ = _emailService.SendContractSuccessEmailAsync(
                    currentUser.Email,
                    currentUser.Profile.FullName, 
                    contractId);      

                if (otherUser != null)
                {
                    _ = _emailService.SendContractSuccessEmailAsync(
                        otherUser.Email,
                        otherUser.Profile.FullName,
                        contractId);
                }
            }
            else
            {
                // Mới 1 bên ký: Bắn tin nhắn tự động gọi bên kia
                string senderName = currentUserId == contract.LessorId ? "Chủ mặt bằng" : "Người thuê";

                var systemMessage = new Message
                {
                    ConversationId = contract.ConversationId,
                    SenderId = currentUserId, 
                    Content = $"{senderName} đã xác nhận hợp đồng. Vui lòng kiểm tra và xác nhận để hoàn tất thủ tục.",
                    MessageType = MessageTypeEnum.SystemAction,
                };

                var conversation = await _unitOfWork.conversationRepository.GetAsync(x => x.Id == contract.ConversationId);
                if (conversation != null)
                {
                    conversation.LastMessage = DateTime.UtcNow;
                    await _unitOfWork.conversationRepository.UpdateAsync(conversation);
                }
                await _unitOfWork.messageRepository.AddAsync(systemMessage);
                returnMessage = systemMessage;
            }
            await _unitOfWork.SaveChangesAsync();
            return new ServiceResult<MessageResponse>
            {
                IsSuccess = true,
                Data = _mapper.Map<MessageResponse>(returnMessage),
                Message = "Đã xác thực hợp đồng thành công"
            };
        }
      

        private async Task<(string? ErrorMessage, Space? Space, PrimaryBookingRequest? Booking, Conversation? conversation)> ValidateRequestAsync(ContractRequest request)
        {
            if (request.Duration <= 0)
            {
                return ("Thời hạn hợp đồng phải lớn hơn 0.", null, null,null);
            }

            if (request.Price <= 0)
            {
                return ("Giá hợp đồng phải lớn hơn 0.", null, null, null) ;
            }

            if (request.DepositAmount < 0)
            {
                return ("Tiền đặt cọc không được nhỏ hơn 0.", null, null, null);
            }

            if (request.Acreage <= 0)
            {
                return ("Diện tích phải lớn hơn 0.", null, null, null);
            }
            // 1. Validate Không gian (Space)
            var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == request.SpaceId && !x.IsDeleted);
            if (space == null)
            {
                return ("Không tìm thấy không gian với Id đã cho.", null, null, null);
            }

            if (string.IsNullOrWhiteSpace(space.OwnerId))
            {
                return ("Không thể xác định chủ sở hữu của không gian.", null, null, null);
            }

            // 2. Validate Yêu cầu đặt chỗ (BookingRequest)
            var primaryBookingRequest = await _unitOfWork.primaryBookingRequestRepository.GetAsync(x => x.Id == request.PrimaryBookingRequestId && !x.IsDeleted);
            if (primaryBookingRequest == null)
            {
                return ("Không tìm thấy yêu cầu đặt chỗ với Id đã cho.", null, null, null);
            }

            if (string.IsNullOrWhiteSpace(primaryBookingRequest.LesseeId))
            {
                return ("Không thể xác định người thuê từ yêu cầu đặt chỗ.", null, null, null);
            }

            if (primaryBookingRequest.SpaceId != request.SpaceId)
            {
                return ("Yêu cầu đặt chỗ phải thuộc đúng không gian của hợp đồng.", null, null, null);
            }
            //3. validate cho conversation 
            var conversation = await _unitOfWork.conversationRepository.GetAsync(x => x.Id == request.ConversationId);  
            if (conversation == null)
            {
                return ("Không tìm thấy cuộc trò chuyện với Id đã cho.", null, null, null);
            }
            if(conversation.LessorId != space.OwnerId || conversation.LesseeId != primaryBookingRequest.LesseeId)
            {
                return ("Cuộc trò chuyện không khớp với chủ sở hữu và người thuê của hợp đồng.", null, null, null);
            }

            // 4. Nếu mọi thứ hợp lệ, ErrorMessage = null, trả về kèm 2 object
            return (null, space, primaryBookingRequest, conversation);
        }
    }
}
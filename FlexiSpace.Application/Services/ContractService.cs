using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Requests.Contract;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace FlexiSpace.Application.Services
{
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public ContractService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
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
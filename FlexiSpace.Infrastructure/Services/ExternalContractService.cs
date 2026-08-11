using AutoMapper;
using FlexiSpace.Application;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Infrastructure.Services
{
    public class ExternalContractService : IExternalContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public ExternalContractService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<ServiceResult<MessageResponse>> CreateAndShareAsync(CreateExternalContractRequest request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Register first!" };
                }

                var validation = await ValidateCreateAsync(request, currentUserId);
                if (validation.ErrorMessage != null)
                {
                    return new ServiceResult<MessageResponse> { IsSuccess = false, Message = validation.ErrorMessage };
                }

                var contract = MapExternalContract(request, currentUserId, validation.Space!);
                contract.ContractVerification = new ContractVerification
                {
                    IsLessorAgreed = true,
                    LessorSignedAt = DateTime.UtcNow,
                    LessorIpAddress = _currentUserService.GetClientIpAddress(),
                    LessorSignatureData = "Uploaded external contract",
                    IsLesseeAgreed = false
                };

                var profileValidation = await PopulateContractParticipantProfilesAsync(contract);
                if (profileValidation != null)
                {
                    return new ServiceResult<MessageResponse> { IsSuccess = false, Message = profileValidation };
                }

                await _unitOfWork.contractRepository.AddAsync(contract);
                await _unitOfWork.SaveChangesAsync();

                var proposalMessage = _mapper.Map<Message>(contract);
                await _unitOfWork.messageRepository.AddAsync(proposalMessage);
                await UpdateConversationLastMessageAsync(request.ConversationId);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ServiceResult<MessageResponse>
                {
                    IsSuccess = true,
                    Message = "External contract uploaded and shared to chat.",
                    Data = _mapper.Map<MessageResponse>(proposalMessage)
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<MessageResponse>> ConfirmAsync(long contractId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Register first!" };
                }

                var contract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == contractId && !x.IsDeleted,
                    include: q => q.Include(c => c.ContractVerification)
                                   .Include(c => c.PictureURLs)
                                   .Include(c => c.Lessor)
                                   .Include(c => c.Lessee));

                if (contract == null)
                {
                    return new ServiceResult<MessageResponse> { IsSuccess = false, IsNotFound = true, Message = "Contract not found." };
                }

                if (contract.Source != ContractSource.External)
                {
                    return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Contract này không phải hợp đồng ngoài hệ thống." };
                }

                if (contract.Status != ContractStatusEnum.PendingExternalVerification)
                {
                    return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Chỉ có thể xác nhận hợp đồng ngoài đang chờ xác thực." };
                }

                contract.ContractVerification ??= new ContractVerification();

                if (currentUserId == contract.LessorId)
                {
                    contract.ContractVerification.IsLessorAgreed = true;
                    contract.ContractVerification.LessorSignedAt = DateTime.UtcNow;
                    contract.ContractVerification.LessorIpAddress = _currentUserService.GetClientIpAddress();
                    contract.ContractVerification.LessorSignatureData = "Verified external contract";
                }
                else if (currentUserId == contract.LesseeId)
                {
                    contract.ContractVerification.IsLesseeAgreed = true;
                    contract.ContractVerification.LesseeSignedAt = DateTime.UtcNow;
                    contract.ContractVerification.LesseeIpAddress = _currentUserService.GetClientIpAddress();
                    contract.ContractVerification.LesseeSignatureData = "Verified external contract";
                }
                else
                {
                    return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Bạn không có quyền xác nhận hợp đồng này." };
                }

                Message systemMessage;
                if (contract.ContractVerification.IsLessorAgreed && contract.ContractVerification.IsLesseeAgreed)
                {
                    contract.Status = ContractStatusEnum.Active;
                    contract.IsActive = true;
                    contract.IsDeleted = false;
                    contract.ContractSnapshot = BuildExternalContractSnapshot(contract);
                    await EnsureSpaceUsageRightAsync(contract);
                    systemMessage = CreateSystemMessage(
                        contract,
                        currentUserId,
                        "Hợp đồng ngoài hệ thống đã được cả hai bên xác nhận và kích hoạt.");
                }
                else
                {
                    var senderName = currentUserId == contract.LessorId ? "Bên cho thuê" : "Bên thuê";
                    systemMessage = CreateSystemMessage(
                        contract,
                        currentUserId,
                        $"{senderName} đã xác nhận hợp đồng ngoài hệ thống. Vui lòng kiểm tra và xác nhận để hoàn tất.");
                }

                contract.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.contractRepository.UpdateAsync(contract);
                await _unitOfWork.messageRepository.AddAsync(systemMessage);
                await UpdateConversationLastMessageAsync(contract.ConversationId);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<MessageResponse>
                {
                    IsSuccess = true,
                    Message = contract.Status == ContractStatusEnum.Active
                        ? "External contract confirmed by both parties and activated."
                        : "External contract confirmation saved.",
                    Data = _mapper.Map<MessageResponse>(systemMessage)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = ex.Message };
            }
        }

        private Contract MapExternalContract(CreateExternalContractRequest request, string currentUserId, Space space)
        {
            return _mapper.Map<Contract>(request, opt =>
            {
                opt.Items["CurrentUserId"] = currentUserId;
                opt.Items["SpaceArea"] = space.Area;
            });
        }

        private async Task<(string? ErrorMessage, Space? Space)> ValidateCreateAsync(CreateExternalContractRequest request, string currentUserId)
        {
            if (request.LesseeId == currentUserId) return ("Bạn không thể tạo hợp đồng thuê cho chính mình.", null);
            if (string.IsNullOrWhiteSpace(request.ConversationId)) return ("Vui lòng cung cấp phòng chat để chia sẻ hợp đồng.", null);

            var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == request.SpaceId && !x.IsDeleted && x.IsActive);
            if (space == null) return ("Không tìm thấy mặt bằng.", null);

            var lessee = await _unitOfWork.userRepository.GetAsync(x => x.UserId == request.LesseeId);
            if (lessee == null) return ("Không tìm thấy người thuê.", null);

            var conversation = await _unitOfWork.conversationRepository.GetAsync(x => x.Id == request.ConversationId);
            if (conversation == null) return ("Không tìm thấy phòng chat.", null);

            if (space.OwnerId == currentUserId) return (null, space);

            var rights = await _unitOfWork.spaceUsageRightRepository.GetAllAsync(x =>
                x.SpaceId == request.SpaceId &&
                x.UserId == currentUserId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.CanShare &&
                x.Type != SpaceUsageRightType.SubRenter);

            return rights.Any()
                ? (null, space)
                : ("Bạn không có quyền tạo hợp đồng ngoài cho mặt bằng này.", null);
        }

        private async Task<string?> PopulateContractParticipantProfilesAsync(Contract contract)
        {
            var lessorProfile = await _unitOfWork.profileRepository.GetAsync(p => p.UserId == contract.LessorId);
            var lesseeProfile = await _unitOfWork.profileRepository.GetAsync(p => p.UserId == contract.LesseeId);

            if (lessorProfile == null || lesseeProfile == null) return "Nguoi tham gia can cap nhat ho so CCCD truoc khi tao hop dong.";
            if (!lessorProfile.IsVerified || !lesseeProfile.IsVerified) return "Ca hai nguoi tham gia can xac thuc CCCD truoc khi tao hop dong.";

            contract.LessorNumberCard = lessorProfile.IdentityCardNumber;
            contract.LessorCardAddress = lessorProfile.PermanentResidence;
            contract.LessorName = lessorProfile.FullName;
            contract.LessorCardIssuanceDate = lessorProfile.DateOfIssue;
            contract.LesseeNumberCard = lesseeProfile.IdentityCardNumber;
            contract.LesseeCardAddress = lesseeProfile.PermanentResidence;
            contract.LesseeName = lesseeProfile.FullName;
            contract.LesseeCardIssuanceDate = lesseeProfile.DateOfIssue;

            return null;
        }

        private async Task EnsureSpaceUsageRightAsync(Contract contract)
        {
            var existed = await _unitOfWork.spaceUsageRightRepository.GetAsync(x => x.ContractId == contract.Id && !x.IsDeleted);
            if (existed != null) return;

            await _unitOfWork.spaceUsageRightRepository.AddAsync(new SpaceUsageRight
            {
                SpaceId = contract.SpaceId,
                ContractId = contract.Id,
                UserId = contract.LesseeId,
                GrantedByUserId = contract.LessorId,
                ValidFrom = contract.StartDate,
                ValidTo = contract.EndDate,
                CanShare = contract.CanShare,
                CanGrantSharePermission = contract.CanGrantSharePermission,
                Type = SpaceUsageRightType.PrimaryRenter,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = contract.LessorId
            });
        }

        private async Task UpdateConversationLastMessageAsync(string? conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId)) return;

            var conversation = await _unitOfWork.conversationRepository.GetAsync(x => x.Id == conversationId);
            if (conversation == null) return;

            conversation.LastMessage = DateTime.UtcNow;
            await _unitOfWork.conversationRepository.UpdateAsync(conversation);
        }

        private static Message CreateSystemMessage(Contract contract, string senderId, string content)
        {
            return new Message
            {
                ConversationId = contract.ConversationId,
                SenderId = senderId,
                Content = content,
                MessageType = MessageTypeEnum.SystemAction,
                CreateAt = DateTime.UtcNow,
                IsRead = false
            };
        }

        private static string BuildExternalContractSnapshot(Contract contract)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                ContractId = contract.Id,
                contract.SpaceId,
                contract.LessorId,
                contract.LesseeId,
                contract.CanShare,
                contract.CanGrantSharePermission,
                DocumentImages = contract.PictureURLs?.Select(x => x.ImageUrl).ToList()
            });
        }
    }
}

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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

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
                if (validation.ErrorMessage != null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = validation.ErrorMessage
                    };
                }
                if (validation.Booking!.LessorId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Lỗi bảo mật: Chỉ chủ sở hữu mặt bằng mới có quyền tạo hợp đồng!"
                    };
                }
    
                var contract = _mapper.Map<Contract>(request);
                contract.LessorId = validation.Booking.LessorId;
                contract.LesseeId = validation.Booking!.LesseeId;
                contract.StartDate = NormalizeTimestampWithoutTimeZone(contract.StartDate);
                contract.EndDate = CalculateEndDate(contract.StartDate, contract.DurationUnit, contract.Duration);
                contract.CreatedAt = DateTime.Now;
                contract.UpdatedAt = DateTime.Now;
                contract.ContractSnapshot = string.Empty;
                contract.Source = ContractSource.Platform;
                contract.Lessor = validation.Booking.Lessor;
                contract.Lessee = validation.Booking.Lessee;
                var profileValidation = await PopulateContractParticipantProfilesAsync(contract);
                if (profileValidation != null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = profileValidation
                    };
                }
                contract.ContractVerification = new ContractVerification
                {
                    IsLessorAgreed = false,
                    IsLesseeAgreed = false,
                };

                SyncContractSchedules(contract, request.ContractSchedules);

                await _unitOfWork.contractRepository.AddAsync(contract);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var response = _mapper.Map<ContractResponse>(contract);
                EnrichContractResponseForCurrentUser(response, contract, currentUserId);

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Data = response
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

        private static DateTime NormalizeTimestampWithoutTimeZone(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        }

        private async Task<string?> PopulateContractParticipantProfilesAsync(Contract contract)
        {
            var lessorProfile = await _unitOfWork.profileRepository.GetAsync(p => p.UserId == contract.LessorId);
            var lesseeProfile = await _unitOfWork.profileRepository.GetAsync(p => p.UserId == contract.LesseeId);

            if (lessorProfile == null || lesseeProfile == null)
            {
                return "Nguoi tham gia can cap nhat ho so CCCD truoc khi tao hop dong.";
            }
            if (!lessorProfile.IsVerified || !lesseeProfile.IsVerified)
            {
                return "Ca hai nguoi tham gia can xac thuc CCCD truoc khi tao hop dong.";
            }

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
        public async Task<ServiceResult<ContractResponse>> StartContractSigningAsync(long contractId, StartContractSigningRequest request)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var contract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == contractId && !x.IsDeleted,
                    include: q => q.Include(c => c.ContractVerification)
                                   .Include(c => c.ContractSchedules)
                                   .Include(c => c.Lessor)
                                   .Include(c => c.Lessee));

                if (contract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Khong tim thay hop dong."
                    };
                }

                if (contract.LessorId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Chi nguoi cho thue moi co quyen bat dau phien ky hop dong."
                    };
                }

                if (contract.LessorId != request.LessorId || contract.LesseeId != request.LesseeId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Hai nguoi tham gia khong khop voi hop dong."
                    };
                }

                if (contract.Status != ContractStatusEnum.Draft)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Chi co the bat dau ky khi hop dong dang o trang thai Draft."
                    };
                }

                contract.ContractVerification ??= new ContractVerification();
                contract.ContractVerification.IsLessorAgreed = false;
                contract.ContractVerification.IsLesseeAgreed = false;
                contract.ContractVerification.LessorSignedAt = null;
                contract.ContractVerification.LesseeSignedAt = null;
                contract.ContractVerification.LessorIpAddress = null;
                contract.ContractVerification.LesseeIpAddress = null;
                contract.ContractVerification.LessorSignatureData = null;
                contract.ContractVerification.LesseeSignatureData = null;

                contract.Status = ContractStatusEnum.Signing;
                contract.UpdatedAt = DateTime.UtcNow;
                contract.PreSignSnapshot = BuildPreSignSnapshot(contract);
                contract.PreSignHash = ComputeSha256Hash(contract.PreSignSnapshot);
                contract.PostSignSnapshot = null;
                contract.PostSignHash = null;

                await _unitOfWork.contractRepository.UpdateAsync(contract);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Message = "Da bat dau phien ky va khoa ban hop dong bang SHA-256.",
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

        public async Task<ServiceResult<ContractResponse>> CancelContractSigningAsync(long contractId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var contract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == contractId && !x.IsDeleted,
                    include: q => q.Include(c => c.ContractVerification));

                if (contract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Khong tim thay hop dong."
                    };
                }

                if (contract.LessorId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Chi nguoi cho thue moi co quyen huy phien ky hop dong."
                    };
                }

                if (contract.Status == ContractStatusEnum.Cancelled)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Hop dong da bi huy nen khong the huy phien ky."
                    };
                }

                if (contract.Status != ContractStatusEnum.Signing)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Chi co the huy phien ky khi hop dong dang o trang thai Signing."
                    };
                }

                ResetSigningSession(contract);
                contract.Status = ContractStatusEnum.Draft;
                contract.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.contractRepository.UpdateAsync(contract);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Message = "Da huy phien ky. Hop dong da quay ve Draft de chinh sua.",
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

        public async Task<ServiceResult<ContractResponse>> CancelContractAsync(long contractId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var contract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == contractId && !x.IsDeleted,
                    include: q => q.Include(c => c.ContractVerification));

                if (contract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Khong tim thay hop dong."
                    };
                }

                if (contract.LessorId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Chi nguoi cho thue moi co quyen huy hop dong."
                    };
                }

                if (contract.Status == ContractStatusEnum.Active)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Hop dong da Active nen khong the huy bang chuc nang nay."
                    };
                }

                if (contract.Status == ContractStatusEnum.Cancelled)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Hop dong da o trang thai Cancelled."
                    };
                }

                ResetSigningSession(contract);
                contract.Status = ContractStatusEnum.Cancelled;
                contract.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.contractRepository.UpdateAsync(contract);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Message = "Da huy hop dong. Hop dong se khong the ky duoc nua.",
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
                    include: q => q
                        .Include(c => c.Space)
                        .Include(c => c.PrimaryBookingRequest)
                        .Include(c => c.Lessee)
                        .Include(c => c.Lessor)
                        .Include(c => c.PictureURLs)
                        .Include(c => c.ContractSchedules));

                var responses = _mapper.Map<List<ContractResponse>>(contracts);
                foreach (var response in responses)
                {
                    var contract = contracts.First(x => x.Id == response.Id);
                    EnrichContractResponseForCurrentUser(response, contract, currentUserId);
                }

                return new ServiceResult<List<ContractResponse>>
                {
                    IsSuccess = true,
                    Data = responses
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
                    include: q => q
                        .Include(c => c.Space)
                        .Include(c => c.PrimaryBookingRequest)
                        .Include(c => c.Lessee)
                        .Include(c => c.Lessor)
                        .Include(c => c.PictureURLs)
                        .Include(c => c.ContractSchedules));

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

                var response = _mapper.Map<ContractResponse>(contract);
                EnrichContractResponseForCurrentUser(response, contract, currentUserId);

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Data = response
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

        private static void EnrichContractResponseForCurrentUser(ContractResponse response, Contract contract, string? currentUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return;
            }

            response.CurrentUserContractRole = contract.LessorId == currentUserId
                ? "Lessor"
                : contract.LesseeId == currentUserId
                    ? "Lessee"
                    : null;

            var isActiveLessee = contract.LesseeId == currentUserId &&
                                 contract.Status == ContractStatusEnum.Active &&
                                 contract.IsActive &&
                                 !contract.IsDeleted;

            response.CurrentUserCanShareSpace = isActiveLessee && contract.CanShare;
            response.CurrentUserCanGrantSharePermission = isActiveLessee && contract.CanGrantSharePermission;
        }

        public async Task<ServiceResult<string>> GetContractSnapshotByIdAsync(long id)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var contract = await _unitOfWork.contractRepository.GetAsync(x => x.Id == id && !x.IsDeleted);

                if (contract == null)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy hợp đồng với Id đã cho."
                    };
                }

                if (contract.LessorId != currentUserId && contract.LesseeId != currentUserId)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "Lỗi bảo mật: Bạn không có quyền truy cập vào hợp đồng này!"
                    };
                }

                if (string.IsNullOrWhiteSpace(contract.ContractSnapshot))
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = false,
                        Message = "Hợp đồng chưa được ký đầy đủ, không có bản sao hợp đồng."
                    };
                }

                return new ServiceResult<string>
                {
                    IsSuccess = true,
                    Data = contract.ContractSnapshot
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

        public async Task<ServiceResult<ContractIntegrityVerificationResponse>> VerifyContractIntegrityAsync(long id)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var contract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(c => c.ContractVerification)
                                   .Include(c => c.ContractSchedules));

                if (contract == null)
                {
                    return new ServiceResult<ContractIntegrityVerificationResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Khong tim thay hop dong."
                    };
                }

                if (contract.LessorId != currentUserId && contract.LesseeId != currentUserId)
                {
                    return new ServiceResult<ContractIntegrityVerificationResponse>
                    {
                        IsSuccess = false,
                        Message = "Ban khong co quyen xac thuc tinh toan ven cua hop dong nay."
                    };
                }

                if (string.IsNullOrWhiteSpace(contract.PostSignHash))
                {
                    return new ServiceResult<ContractIntegrityVerificationResponse>
                    {
                        IsSuccess = false,
                        Message = "Hop dong chua co PostSignHash de xac thuc toan ven."
                    };
                }

                // 1. Lấy chuỗi Snapshot mới và cũ ra các biến độc lập để có thể so sánh text
                var newPostSignSnapshot = BuildPostSignSnapshot(contract);
                var oldPostSignSnapshot = contract.PostSignSnapshot ?? "";

                var newPostSignHash = ComputeSha256Hash(newPostSignSnapshot);
                var storedPostSignSnapshotHash = !string.IsNullOrWhiteSpace(oldPostSignSnapshot)
                    ? ComputeSha256Hash(oldPostSignSnapshot)
                    : null;

                var isStoredSnapshotMatched = !string.IsNullOrWhiteSpace(storedPostSignSnapshotHash)
                    && string.Equals(storedPostSignSnapshotHash, contract.PostSignHash, StringComparison.OrdinalIgnoreCase);
                var isMatched = string.Equals(newPostSignHash, contract.PostSignHash, StringComparison.OrdinalIgnoreCase);
                var isTampered = !isMatched;
                var report = new ContractIntegrityVerificationResponse
                {
                    ContractId = contract.Id,
                    OldPostSignHash = contract.PostSignHash,
                    NewPostSignHash = newPostSignHash,
                    StoredPostSignSnapshotHash = storedPostSignSnapshotHash,
                    IsStoredSnapshotMatched = isStoredSnapshotMatched,
                    IsMatched = isMatched,
                    IsTampered = isTampered,
                    VerifiedAt = DateTime.UtcNow
                };

                return new ServiceResult<ContractIntegrityVerificationResponse>
                {
                    IsSuccess = true,
                    Message = report.Verdict,
                    Data = report
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ContractIntegrityVerificationResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<List<ContractCalendarEntryResponse>>> GetContractCalendarBySpaceAsync(long spaceId, DateTime from, DateTime to)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (from.Date > to.Date)
                {
                    return new ServiceResult<List<ContractCalendarEntryResponse>>
                    {
                        IsSuccess = false,
                        Message = "Ngày bắt đầu không được lớn hơn ngày kết thúc."
                    };
                }

                var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == spaceId && !x.IsDeleted);

                if (space == null)
                {
                    return new ServiceResult<List<ContractCalendarEntryResponse>> { IsSuccess = false, IsNotFound = true, Message = "Không tìm thấy mặt bằng." };
                }

                // 1. Lấy tất cả hợp đồng đang Active của mặt bằng này
                var contracts = await _unitOfWork.contractRepository.GetAllAsync(
                    x => !x.IsDeleted && x.SpaceId == spaceId && x.Status == ContractStatusEnum.Active,
                    include: q => q.Include(c => c.ContractSchedules).Include(c => c.PrimaryBookingRequest));

                // 2. SỬA PHÂN QUYỀN: Kiểm tra xem user hiện tại là Chủ mặt bằng HAY là một trong những Người thuê
                bool isOwner = space.OwnerId == currentUserId;
                bool isLessee = contracts.Any(c => c.LesseeId == currentUserId);

                if (!isOwner && !isLessee)
                {
                    return new ServiceResult<List<ContractCalendarEntryResponse>>
                    {
                        IsSuccess = false,
                        Message = "Bạn không có quyền xem lịch của mặt bằng này. Chỉ chủ mặt bằng và những người đang thuê mới được phép."
                    };
                }

                var entries = new List<ContractCalendarEntryResponse>();

                foreach (var contract in contracts)
                {
                    var tenantName = !string.IsNullOrWhiteSpace(contract.LesseeName) ? contract.LesseeName : "Người thuê";
                    var businessDescription = !string.IsNullOrWhiteSpace(contract.BusinessPurpose) ? contract.BusinessPurpose : "Thuê mặt bằng";

                    var effectiveFrom = contract.StartDate.Date;
                    var effectiveTo = contract.EndDate.Date;

                    // Bỏ qua nếu hợp đồng nằm ngoài vùng query
                    if (effectiveTo < from.Date || effectiveFrom > to.Date) continue;

                    var startDate = from.Date > effectiveFrom ? from.Date : effectiveFrom;
                    var endDate = to.Date < effectiveTo ? to.Date : effectiveTo;

                    for (var day = startDate; day <= endDate; day = day.AddDays(1))
                    {
                        var weekday = day.DayOfWeek;

                        // 3. SỬA LỖI LỌT CA: Dùng Where để lấy TẤT CẢ các ca trong một ngày
                        var matchingSchedules = (contract.ContractSchedules ?? Enumerable.Empty<ContractSchedule>())
                            .Where(s => s.DayOfWeek == weekday)
                            .ToList();

                        if (!matchingSchedules.Any()) continue;

                        foreach (var schedule in matchingSchedules)
                        {
                            var startDateTime = day.Add(schedule.StartTime);
                            var endDateTime = day.Add(schedule.EndTime);

                            entries.Add(new ContractCalendarEntryResponse
                            {
                                EffectiveDate = day,
                                StartDateTime = startDateTime,
                                EndDateTime = endDateTime,
                                ContractId = contract.Id,
                                TenantName = tenantName,
                                BusinessDescription = businessDescription,
                                DisplayLabel = $"{tenantName} - {businessDescription}"
                            });
                        }
                    }
                }

                return new ServiceResult<List<ContractCalendarEntryResponse>>
                {
                    IsSuccess = true,
                    // Order theo thời gian bắt đầu để Frontend dễ render
                    Data = entries.OrderBy(e => e.StartDateTime).ToList()
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<ContractCalendarEntryResponse>> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<ContractResponse>> UpdateContractAsync(long id, ContractRequest request)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;

                var existingContract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(c => c.Space).Include(c => c.PrimaryBookingRequest).Include(c => c.ContractSchedules).Include(c => c.Lessee).Include(c => c.Lessor));

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

                if (existingContract.Status != ContractStatusEnum.Draft)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Chi co the cap nhat hop dong o trang thai Draft."
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
                if (validation.Booking!.LessorId != currentUserId)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Lỗi bảo mật: Mặt bằng mới không thuộc sở hữu của bạn!"
                    };
                }

                _mapper.Map(request, existingContract);
                SyncContractSchedules(existingContract, request.ContractSchedules);
                existingContract.LessorId = validation.Booking.LessorId;
                existingContract.Lessor = validation.Booking.Lessor;
                existingContract.Lessee = validation.Booking!.Lessee;
                existingContract.EndDate = CalculateEndDate(existingContract.StartDate, existingContract.DurationUnit, existingContract.Duration);
                existingContract.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.contractRepository.UpdateAsync(existingContract);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<ContractResponse>(existingContract);
                EnrichContractResponseForCurrentUser(response, existingContract, currentUserId);

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Data = response
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
                if (existingContract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy hợp đồng với Id đã cho."
                    };
                }
                if(existingContract.Status != ContractStatusEnum.Draft)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = "Chỉ có thể xóa hợp đồng ở trạng thái Nháp (Draft)."
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
            if (contract.Status != ContractStatusEnum.Signing)
                return new ServiceResult<bool> { IsSuccess = false, Message = "Hop dong chua duoc chuyen sang phien ky." };
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
                               .Include(c => c.ContractSchedules)
                               .Include(c => c.PrimaryBookingRequest)
                                   .ThenInclude(b => b.Listing)
                                       .ThenInclude(l => l.ShareSpaceDetail)
            );
            if (contract == null || contract.ContractVerification == null)
            {
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Hợp đồng không tồn tại hoặc dữ liệu bị lỗi." };
            }
            if (contract.Status != ContractStatusEnum.Signing)
            {
                await ResetContractToDraftAfterSigningFailureAsync(contract);
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Hợp đồng chưa được chuyển sang phiên ký." };
            }
            if (string.IsNullOrWhiteSpace(contract.PreSignSnapshot) || string.IsNullOrWhiteSpace(contract.PreSignHash))
            {
                await ResetContractToDraftAfterSigningFailureAsync(contract);
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Phiên kí chưa có bản hash gốc. Vui lòng bắt đầu lại phiên ký." };
            }
            var currentPreSignHash = ComputeSha256Hash(BuildPreSignSnapshot(contract));
            if (!string.Equals(currentPreSignHash, contract.PreSignHash, StringComparison.OrdinalIgnoreCase))
            {
                await ResetContractToDraftAfterSigningFailureAsync(contract);
                return new ServiceResult<MessageResponse> { IsSuccess = false, Message = "Hợp đồng đã có sự thay đổi so với bản gốc. Phiên ký hiện tại đã bị hủy." };
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
                contract.IsActive = true;
                contract.IsDeleted = false;
                contract.PostSignSnapshot = BuildPostSignSnapshot(contract);
                contract.PostSignHash = ComputeSha256Hash(contract.PostSignSnapshot);
                contract.ContractSnapshot = contract.PostSignSnapshot;
                await EnsureSpaceUsageRightAsync(contract);
                await MarkListingOccupiedAfterSuccessfulRentalAsync(contract);
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

        private async Task MarkListingOccupiedAfterSuccessfulRentalAsync(Contract contract)
        {
            var listing = contract.PrimaryBookingRequest?.Listing;
            if (listing == null && contract.PrimaryBookingRequestId.HasValue)
            {
                var booking = await _unitOfWork.primaryBookingRequestRepository.GetAsync(
                    x => x.Id == contract.PrimaryBookingRequestId.Value && !x.IsDeleted,
                    include: q => q.Include(b => b.Listing).ThenInclude(l => l.ShareSpaceDetail));
                listing = booking?.Listing;
            }

            if (listing == null || listing.Status != ListingStatusEnum.Available)
            {
                return;
            }

            if (listing.ListingType == ListingType.SharedSpace)
            {
                if (listing.ShareSpaceDetail == null)
                {
                    return;
                }

                if (listing.ShareSpaceDetail.MaxSubRenter > 0)
                {
                    listing.ShareSpaceDetail.MaxSubRenter -= 1;
                }

                if (listing.ShareSpaceDetail.MaxSubRenter <= 0)
                {
                    listing.Status = ListingStatusEnum.Occupied;
                    listing.IsActive = false;
                }
            }
            else
            {
                listing.Status = ListingStatusEnum.Occupied;
                listing.IsActive = false;
            }

            listing.UpdatedAt = DateTime.Now;
            listing.UpdatedBy = "SystemContractSigning";
            await _unitOfWork.listingRepository.UpdateAsync(listing);
            await InvalidateListingCacheAsync(listing.Id);
        }

        private async Task InvalidateListingCacheAsync(long listingId)
        {
            var keysToInvalidate = new List<string>
            {
                $"Cache:Listing:Id_{listingId}"
            };

            var allStatuses = Enum.GetValues<ListingStatusEnum>().Cast<ListingStatusEnum?>().ToList();
            allStatuses.Add(null);
            var allTypes = Enum.GetValues<ListingType>().Cast<ListingType?>().ToList();
            allTypes.Add(null);

            foreach (var status in allStatuses)
            {
                foreach (var type in allTypes)
                {
                    keysToInvalidate.Add($"Cache:Listings:Status_{status?.ToString() ?? "All"}:Type_{type?.ToString() ?? "All"}");
                }
            }

            foreach (var key in keysToInvalidate.Distinct())
            {
                await _cache.RemoveAsync(key);
            }
        }
      

        private void SyncContractSchedules(Contract contract, List<ContractScheduleRequest>? scheduleRequests)
        {
            if (contract.ContractSchedules == null)
            {
                contract.ContractSchedules = new List<ContractSchedule>();
            }
            else
            {
                contract.ContractSchedules.Clear();
            }

            if (scheduleRequests == null || !scheduleRequests.Any())
            {
                return;
            }

            foreach (var scheduleRequest in scheduleRequests)
            {
                contract.ContractSchedules.Add(new ContractSchedule
                {
                    DayOfWeek = scheduleRequest.DayOfWeek,
                    StartTime = scheduleRequest.StartTime,
                    EndTime = scheduleRequest.EndTime,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                });
            }
        }

        private static void ResetSigningSession(Contract contract)
        {
            contract.PreSignSnapshot = null;
            contract.PreSignHash = null;
            contract.PostSignSnapshot = null;
            contract.PostSignHash = null;

            if (contract.ContractVerification == null)
            {
                return;
            }

            contract.ContractVerification.IsLessorAgreed = false;
            contract.ContractVerification.IsLesseeAgreed = false;
            contract.ContractVerification.LessorSignedAt = null;
            contract.ContractVerification.LesseeSignedAt = null;
            contract.ContractVerification.LessorIpAddress = null;
            contract.ContractVerification.LesseeIpAddress = null;
            contract.ContractVerification.LessorSignatureData = null;
            contract.ContractVerification.LesseeSignatureData = null;
        }

        private async Task ResetContractToDraftAfterSigningFailureAsync(Contract contract)
        {
            ResetSigningSession(contract);
            contract.Status = ContractStatusEnum.Draft;
            contract.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.contractRepository.UpdateAsync(contract);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task EnsureSpaceUsageRightAsync(Contract contract)
        {
            var existed = await _unitOfWork.spaceUsageRightRepository.GetAsync(x => x.ContractId == contract.Id && !x.IsDeleted);
            if (existed != null)
            {
                return;
            }

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

        private string BuildPreSignSnapshot(Contract contract)
        {
            var snapshotPayload = new
            {
                ContractId = contract.Id,
                LessorId = contract.LessorId,
                LesseeId = contract.LesseeId,
                SpaceId = contract.SpaceId,
                PrimaryBookingRequestId = contract.PrimaryBookingRequestId,
                ConversationId = contract.ConversationId,
                Date = contract.Date,
                LessorNumberCard = contract.LessorNumberCard,
                LessorName = contract.LessorName,
                LessorCardIssuanceDate = contract.LessorCardIssuanceDate,
                LessorCardAddress = contract.LessorCardAddress,
                LesseeNumberCard = contract.LesseeNumberCard,
                LesseeName = contract.LesseeName,
                LesseeCardIssuanceDate = contract.LesseeCardIssuanceDate,
                LesseeCardAddress = contract.LesseeCardAddress,
                Description = contract.Description,
                BusinessPurpose = contract.BusinessPurpose,
                Acreage = contract.Acreage,
                DurationUnit = contract.DurationUnit,
                Duration = contract.Duration,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                DepositAmount = contract.DepositAmount,
                Price = contract.Price,
                CanShare = contract.CanShare,
                CanGrantSharePermission = contract.CanGrantSharePermission,
                Source = contract.Source,
                Status = contract.Status,
                ContractSchedules = BuildScheduleSnapshot(contract)
            };

            return JsonSerializer.Serialize(snapshotPayload, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        private string BuildPostSignSnapshot(Contract contract)
        {
            return BuildContractSnapshot(contract);
        }

        private string BuildContractSnapshot(Contract contract)
        {
            var snapshotPayload = new
            {
                ContractId = contract.Id,
                LessorId = contract.LessorId,
                LesseeId = contract.LesseeId,
                SpaceId = contract.SpaceId,
                PrimaryBookingRequestId = contract.PrimaryBookingRequestId,
                ConversationId = contract.ConversationId,
                Date = contract.Date,
                LessorNumberCard = contract.LessorNumberCard,
                LessorName = contract.LessorName,
                LessorCardIssuanceDate = contract.LessorCardIssuanceDate,
                LessorCardAddress = contract.LessorCardAddress,
                LesseeNumberCard = contract.LesseeNumberCard,
                LesseeName = contract.LesseeName,
                LesseeCardIssuanceDate = contract.LesseeCardIssuanceDate,
                LesseeCardAddress = contract.LesseeCardAddress,
                Description = contract.Description,
                BusinessPurpose = contract.BusinessPurpose,
                Acreage = contract.Acreage,
                DurationUnit = contract.DurationUnit,
                Duration = contract.Duration,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                DepositAmount = contract.DepositAmount,
                Price = contract.Price,
                CanShare = contract.CanShare,
                CanGrantSharePermission = contract.CanGrantSharePermission,
                Source = contract.Source,
                Status = contract.Status,
                ContractVerification = new
                {
                    contract.ContractVerification?.IsLessorAgreed,
                    contract.ContractVerification?.IsLesseeAgreed,
                    LessorSignedAt = FormatSnapshotDateTime(contract.ContractVerification?.LessorSignedAt),
                    LesseeSignedAt = FormatSnapshotDateTime(contract.ContractVerification?.LesseeSignedAt),
                    contract.ContractVerification?.LessorIpAddress,
                    contract.ContractVerification?.LesseeIpAddress,
                    contract.ContractVerification?.LessorSignatureData,
                    contract.ContractVerification?.LesseeSignatureData
                },
                ContractSchedules = BuildScheduleSnapshot(contract)
            };

            return JsonSerializer.Serialize(snapshotPayload, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        private static string ComputeSha256Hash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string? FormatSnapshotDateTime(DateTime? value)
        {
            return value?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");
        }

        private static List<object>? BuildScheduleSnapshot(Contract contract)
        {
            return contract.ContractSchedules?
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ThenBy(s => s.EndTime)
                .Select(s => new
                {
                    s.DayOfWeek,
                    s.StartTime,
                    s.EndTime
                })
                .Cast<object>()
                .ToList();
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
            var space = await _unitOfWork.spaceRepository.GetAsync(
                x => x.Id == request.SpaceId && !x.IsDeleted,
                include: q => q.Include(x => x.Owner));
            if (space == null)
            {
                return ("Không tìm thấy không gian với Id đã cho.", null, null, null);
            }

            if (string.IsNullOrWhiteSpace(space.OwnerId))
            {
                return ("Không thể xác định chủ sở hữu của không gian.", null, null, null);
            }

            // 2. Validate Yêu cầu đặt chỗ (BookingRequest)
            var primaryBookingRequest = await _unitOfWork.primaryBookingRequestRepository.GetAsync(
                x => x.Id == request.PrimaryBookingRequestId && !x.IsDeleted,
                include: q => q.Include(x => x.Lessee).Include(x => x.Lessor));
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
            if(conversation.LessorId != primaryBookingRequest.LessorId || conversation.LesseeId != primaryBookingRequest.LesseeId)
            {
                return ("Cuộc trò chuyện không khớp với chủ sở hữu và người thuê của hợp đồng.", null, null, null);
            }

            // 4. Nếu mọi thứ hợp lệ, ErrorMessage = null, trả về kèm 2 object
            return (null, space, primaryBookingRequest, conversation);
        }

        public async Task<ServiceResult<int>> DeactivateExpiredContractsAsync()
        {
            try
            {
                var now = DateTime.Now;
                var expiredContracts = await _unitOfWork.contractRepository.GetAllAsync(x =>
                    !x.IsDeleted &&
                    x.Status == ContractStatusEnum.Active &&
                    x.EndDate < now);

                if (!expiredContracts.Any())
                {
                    return new ServiceResult<int>
                    {
                        IsSuccess = true,
                        Data = 0,
                        Message = "Không có hợp đồng nào hết hạn cần vô hiệu hóa."
                    };
                }

                int count = 0;
                foreach (var contract in expiredContracts)
                {
                    contract.Status = ContractStatusEnum.Expired;
                    contract.IsActive = false;
                    contract.UpdatedAt = DateTime.Now;
                    contract.UpdatedBy = "SystemBackgroundWorker";
                    await _unitOfWork.contractRepository.UpdateAsync(contract);
                    count++;
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<int>
                {
                    IsSuccess = true,
                    Data = count,
                    Message = $"Đã tự động chuyển trạng thái của {count} hợp đồng hết hạn thành Expired."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<int>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<SubleaseContractInfoResponse>> GetSubleaseContractByListingIdAsync(long listingId)
        {
            try
            {
                var listing = await _unitOfWork.listingRepository.GetAsync(
                    x => x.Id == listingId && !x.IsDeleted,
                    include: q => q.Include(l => l.Space).Include(l => l.Lessor));

                if (listing == null)
                {
                    return new ServiceResult<SubleaseContractInfoResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy bài đăng với Id đã cho."
                    };
                }

                var contracts = await _unitOfWork.contractRepository.GetAllAsync(
                    x => x.SpaceId == listing.SpaceId && !x.IsDeleted,
                    include: q => q.Include(c => c.Lessor).Include(c => c.Lessee));

                var contract = contracts
                    .Where(x => x.LesseeId == listing.CreatorId)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                if (contract == null)
                {
                    contract = contracts
                        .Where(x => x.CanShare)
                        .OrderByDescending(x => x.CreatedAt)
                        .FirstOrDefault();
                }

                if (contract == null)
                {
                    return new ServiceResult<SubleaseContractInfoResponse>
                    {
                        IsSuccess = true,
                        Data = new SubleaseContractInfoResponse
                        {
                            HasContract = false,
                            ListingId = listingId,
                            SpaceId = listing.SpaceId,
                            Message = "Không tìm thấy hợp đồng cho thuê lại của bài đăng này."
                        }
                    };
                }

                var response = new SubleaseContractInfoResponse
                {
                    HasContract = true,
                    ContractId = contract.Id,
                    ListingId = listingId,
                    SpaceId = contract.SpaceId,
                    LessorId = contract.LessorId,
                    LessorName = !string.IsNullOrWhiteSpace(contract.LessorName) ? contract.LessorName : contract.Lessor?.Profile?.FullName,
                    LesseeId = contract.LesseeId,
                    LesseeName = !string.IsNullOrWhiteSpace(contract.LesseeName) ? contract.LesseeName : contract.Lessee?.Profile?.FullName,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    CanShare = contract.CanShare,
                    Acreage = contract.Acreage,
                    Status = contract.Status,
                    Source = contract.Source,
                    Message = "Lấy thông tin hợp đồng cho thuê lại thành công."
                };

                return new ServiceResult<SubleaseContractInfoResponse>
                {
                    IsSuccess = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<SubleaseContractInfoResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
    }
}

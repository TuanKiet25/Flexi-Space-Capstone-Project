using AutoMapper;
using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Requests.Contract;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class ContractServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IContractRepository> _mockContractRepository;
        private readonly Mock<ISpaceRepository> _mockSpaceRepository;
        private readonly Mock<IPrimaryBookingRequestRepository> _mockBookingRepository;
        private readonly Mock<IConversationRepository> _mockConversationRepository;
        private readonly Mock<IMessageRepository> _mockMessageRepository;
        private readonly Mock<IProfileRepository> _mockProfileRepository;
        private readonly Mock<IContractVerificationRepository> _mockContractVerificationRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly IDistributedCache _cache;
        private readonly ContractService _sut;

        public ContractServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockContractRepository = new Mock<IContractRepository>();
            _mockSpaceRepository = new Mock<ISpaceRepository>();
            _mockBookingRepository = new Mock<IPrimaryBookingRequestRepository>();
            _mockConversationRepository = new Mock<IConversationRepository>();
            _mockMessageRepository = new Mock<IMessageRepository>();
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mockContractVerificationRepository = new Mock<IContractVerificationRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockEmailService = new Mock<IEmailService>();
            _cache = new TestDistributedCache();

            _mockUnitOfWork.SetupGet(u => u.contractRepository).Returns(_mockContractRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.spaceRepository).Returns(_mockSpaceRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.primaryBookingRequestRepository).Returns(_mockBookingRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.conversationRepository).Returns(_mockConversationRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.messageRepository).Returns(_mockMessageRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.profileRepository).Returns(_mockProfileRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.contractVerificationRepository).Returns(_mockContractVerificationRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.userRepository).Returns(_mockUserRepository.Object);

            _sut = new ContractService(_mockUnitOfWork.Object, _mockMapper.Object, _mockCurrentUserService.Object, _cache, _mockEmailService.Object);
        }

        [Fact]
        public async Task CreateContractAsync_InvalidDuration_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var request = CreateContractRequest();
            request.Duration = 0;

            // 2. ACT
            var result = await _sut.CreateContractAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Thời hạn");
            _mockContractRepository.Verify(r => r.AddAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task CreateContractAsync_ValidRequest_CreatesContractWithSchedules()
        {
            // 1. ARRANGE
            var request = CreateContractRequest();
            request.StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
            var contract = new Contract
            {
                Id = 7,
                SpaceId = request.SpaceId,
                PrimaryBookingRequestId = request.PrimaryBookingRequestId,
                ConversationId = request.ConversationId,
                StartDate = request.StartDate,
                Duration = request.Duration,
                DurationUnit = request.DurationUnit,
                Price = request.Price,
                DepositAmount = request.DepositAmount ?? 0,
                Acreage = request.Acreage
            };
            var response = new ContractResponse { Id = 7 };
            SetupValidContractRequestValidation(request);
            SetupParticipantProfiles();
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockMapper.Setup(m => m.Map<Contract>(request)).Returns(contract);
            _mockContractRepository.Setup(r => r.AddAsync(contract)).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<ContractResponse>(contract)).Returns(() => new ContractResponse
            {
                Id = 7,
                LessorId = contract.LessorId,
                LesseeId = contract.LesseeId,
                LessorNumberCard = contract.LessorNumberCard,
                LessorName = contract.LessorName,
                LessorCardIssuanceDate = contract.LessorCardIssuanceDate,
                LessorCardAddress = contract.LessorCardAddress,
                LesseeNumberCard = contract.LesseeNumberCard,
                LesseeName = contract.LesseeName,
                LesseeCardIssuanceDate = contract.LesseeCardIssuanceDate,
                LesseeCardAddress = contract.LesseeCardAddress,
                LessorNickName = contract.Lessor?.Name,
                LesseeNickName = contract.Lessee?.Name
            });

            // 2. ACT
            var result = await _sut.CreateContractAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.LessorNumberCard.Should().Be("111");
            result.Data.LesseeNumberCard.Should().Be("222");
            contract.LessorId.Should().Be("lessor-1");
            contract.LesseeId.Should().Be("lessee-1");
            contract.LessorNumberCard.Should().Be("111");
            contract.LesseeNumberCard.Should().Be("222");
            result.Data!.LessorName.Should().Be("Lessor Full Name");
            result.Data.LesseeName.Should().Be("Lessee Full Name");
            result.Data.LessorCardAddress.Should().Be("Lessor Address");
            result.Data.LesseeCardAddress.Should().Be("Lessee Address");
            contract.StartDate.Kind.Should().Be(DateTimeKind.Unspecified);
            contract.EndDate.Should().Be(request.StartDate.AddMonths(1));
            contract.ContractVerification.Should().NotBeNull();
            contract.ContractSchedules.Should().HaveCount(1);
            _mockContractRepository.Verify(r => r.AddAsync(contract), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllContractsAsync_ContractsHaveUsers_ReturnsResponsesWithNickNames()
        {
            // 1. ARRANGE
            var contracts = new List<Contract>
            {
                new()
                {
                    Id = 7,
                    LessorId = "lessor-1",
                    LesseeId = "lessee-1",
                    Lessor = new User { UserId = "lessor-1", Name = "Lessor Name" },
                    Lessee = new User { UserId = "lessee-1", Name = "Lessee Name" }
                }
            };
            var responses = new List<ContractResponse>
            {
                new() { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1", LessorNickName = "Lessor Name", LesseeNickName = "Lessee Name" }
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contracts);
            _mockMapper
                .Setup(m => m.Map<List<ContractResponse>>(contracts))
                .Returns(responses);

            // 2. ACT
            var result = await _sut.GetAllContractsAsync(new FilterGetAllContract());

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(responses);
            result.Data!.First().LessorNickName.Should().Be("Lessor Name");
            result.Data.First().LesseeNickName.Should().Be("Lessee Name");
        }

        [Fact]
        public async Task GetContractByIdAsync_CurrentUserIsParticipant_ReturnsResponseWithNickNames()
        {
            // 1. ARRANGE
            var contract = new Contract
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                Lessor = new User { UserId = "lessor-1", Name = "Lessor Name" },
                Lessee = new User { UserId = "lessee-1", Name = "Lessee Name" }
            };
            var response = new ContractResponse
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                LessorNickName = "Lessor Name",
                LesseeNickName = "Lessee Name"
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contract);
            _mockMapper
                .Setup(m => m.Map<ContractResponse>(contract))
                .Returns(response);

            // 2. ACT
            var result = await _sut.GetContractByIdAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(response);
            result.Data!.LessorNickName.Should().Be("Lessor Name");
            result.Data.LesseeNickName.Should().Be("Lessee Name");
        }

        [Fact]
        public async Task ShareContractAsync_ContractNotFound_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync((Contract)null!);

            // 2. ACT
            var result = await _sut.ShareContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy");
            _mockMessageRepository.Verify(r => r.AddAsync(It.IsAny<Message>()), Times.Never);
        }

        [Fact]
        public async Task GetContractByIdAsync_CurrentUserNotParticipant_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("other-user");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1" });

            // 2. ACT
            var result = await _sut.GetContractByIdAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không có quyền");
        }

        [Fact]
        public async Task GetContractSnapshotByIdAsync_MissingSnapshot_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1", ContractSnapshot = "" });

            // 2. ACT
            var result = await _sut.GetContractSnapshotByIdAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("chưa được ký");
        }

        [Fact]
        public async Task GetContractCalendarBySpaceAsync_CurrentUserIsOwner_ReturnsCalendarEntries()
        {
            // 1. ARRANGE
            var from = new DateTime(2026, 8, 3);
            var to = new DateTime(2026, 8, 3);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()))
                .ReturnsAsync(new Space { Id = 10, OwnerId = "lessor-1" });
            _mockContractRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(new List<Contract>
                {
                    new()
                    {
                        Id = 7,
                        SpaceId = 10,
                        LessorId = "lessor-1",
                        LesseeId = "lessee-1",
                        Status = ContractStatusEnum.Active,
                        StartDate = from,
                        EndDate = to,
                        LesseeName = "Tenant",
                        BusinessPurpose = "Office",
                        ContractSchedules = new List<ContractSchedule>
                        {
                            new() { DayOfWeek = DayOfWeek.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) }
                        }
                    }
                });

            // 2. ACT
            var result = await _sut.GetContractCalendarBySpaceAsync(10, from, to);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().ContainSingle();
            result.Data!.First().ContractId.Should().Be(7);
            result.Data.First().DisplayLabel.Should().Be("Tenant - Office");
        }

        [Fact]
        public async Task GetContractCalendarBySpaceAsync_CurrentUserIsLessee_ReturnsSharedCalendarEntries()
        {
            // 1. ARRANGE
            var from = new DateTime(2026, 8, 3);
            var to = new DateTime(2026, 8, 3);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()))
                .ReturnsAsync(new Space { Id = 10, OwnerId = "lessor-1" });
            _mockContractRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(new List<Contract>
                {
                    new()
                    {
                        Id = 7,
                        SpaceId = 10,
                        LessorId = "lessor-1",
                        LesseeId = "lessee-1",
                        Status = ContractStatusEnum.Active,
                        StartDate = from,
                        EndDate = to,
                        LesseeName = "Tenant One",
                        BusinessPurpose = "Office",
                        ContractSchedules = new List<ContractSchedule>
                        {
                            new() { DayOfWeek = DayOfWeek.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) }
                        }
                    },
                    new()
                    {
                        Id = 8,
                        SpaceId = 10,
                        LessorId = "lessor-1",
                        LesseeId = "lessee-2",
                        Status = ContractStatusEnum.Active,
                        StartDate = from,
                        EndDate = to,
                        LesseeName = "Tenant Two",
                        BusinessPurpose = "Retail",
                        ContractSchedules = new List<ContractSchedule>
                        {
                            new() { DayOfWeek = DayOfWeek.Monday, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(15) }
                        }
                    }
                });

            // 2. ACT
            var result = await _sut.GetContractCalendarBySpaceAsync(10, from, to);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data!.Select(e => e.ContractId).Should().Contain(new[] { 7L, 8L });
            result.Data.Select(e => e.DisplayLabel).Should().Contain(new[] { "Tenant One - Office", "Tenant Two - Retail" });
        }

        [Fact]
        public async Task DeleteContractAsync_ContractNotFound_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync((Contract)null!);

            // 2. ACT
            var result = await _sut.DeleteContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Contain("Không tìm thấy");
        }

        [Fact]
        public async Task SendContractOtpAsync_DoesNotCheckProfileVerification()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1", Status = ContractStatusEnum.Draft });

            // 2. ACT
            var result = await _sut.SendContractOtpAsync(7);
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("phien ky");
            _mockProfileRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<UserProfile, bool>>>()), Times.Never);
            _mockEmailService.Verify(e => e.SendContractOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
            return;

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("xác thực CCCD");
            _mockEmailService.Verify(e => e.SendContractOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task StartContractSigningAsync_ValidDraft_LocksPreSignSnapshotAndHash()
        {
            // 1. ARRANGE
            var contract = new Contract
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                Status = ContractStatusEnum.Draft,
                Description = "Contract",
                BusinessPurpose = "Office",
                ContractVerification = new ContractVerification(),
                ContractSchedules = new List<ContractSchedule>
                {
                    new() { DayOfWeek = DayOfWeek.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) }
                }
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contract);
            _mockMapper.Setup(m => m.Map<ContractResponse>(contract)).Returns(() => new ContractResponse
            {
                Id = contract.Id,
                Status = contract.Status,
                PreSignHash = contract.PreSignHash
            });

            // 2. ACT
            var result = await _sut.StartContractSigningAsync(7, new StartContractSigningRequest
            {
                LessorId = "lessor-1",
                LesseeId = "lessee-1"
            });

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            contract.Status.Should().Be(ContractStatusEnum.Signing);
            contract.PreSignSnapshot.Should().NotBeNullOrWhiteSpace();
            contract.PreSignSnapshot.Should().NotContain("ContractVerification");
            contract.PreSignSnapshot.Should().NotContain("CreatedAt");
            contract.PreSignSnapshot.Should().NotContain("UpdatedAt");
            contract.PreSignHash.Should().HaveLength(64);
            _mockContractRepository.Verify(r => r.UpdateAsync(contract), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelContractSigningAsync_SigningContract_ReturnsDraftAndClearsSigningData()
        {
            // 1. ARRANGE
            var contract = new Contract
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                Status = ContractStatusEnum.Signing,
                PreSignSnapshot = "{}",
                PreSignHash = new string('a', 64),
                ContractVerification = new ContractVerification
                {
                    IsLessorAgreed = true,
                    LessorSignedAt = DateTime.UtcNow,
                    LessorIpAddress = "127.0.0.1",
                    LessorSignatureData = "Verified via Email OTP"
                }
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contract);
            _mockMapper.Setup(m => m.Map<ContractResponse>(contract)).Returns(() => new ContractResponse { Id = contract.Id, Status = contract.Status });

            // 2. ACT
            var result = await _sut.CancelContractSigningAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            contract.Status.Should().Be(ContractStatusEnum.Draft);
            contract.PreSignSnapshot.Should().BeNull();
            contract.PreSignHash.Should().BeNull();
            contract.ContractVerification.IsLessorAgreed.Should().BeFalse();
            contract.ContractVerification.LessorSignedAt.Should().BeNull();
            _mockContractRepository.Verify(r => r.UpdateAsync(contract), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelContractAsync_DraftContract_ReturnsCancelledAndPreventsSigning()
        {
            // 1. ARRANGE
            var contract = new Contract
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                Status = ContractStatusEnum.Draft,
                ContractVerification = new ContractVerification()
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contract);
            _mockMapper.Setup(m => m.Map<ContractResponse>(contract)).Returns(() => new ContractResponse { Id = contract.Id, Status = contract.Status });

            // 2. ACT
            var result = await _sut.CancelContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            contract.Status.Should().Be(ContractStatusEnum.Cancelled);
            _mockContractRepository.Verify(r => r.UpdateAsync(contract), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyContractIntegrityAsync_CurrentPostSignHashMatchesStoredHash_ReturnsSafeReport()
        {
            // 1. ARRANGE
            var contract = new Contract
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                Status = ContractStatusEnum.Active,
                Description = "Contract",
                BusinessPurpose = "Office",
                ContractVerification = new ContractVerification { IsLessorAgreed = true, IsLesseeAgreed = true },
                ContractSchedules = new List<ContractSchedule>()
            };
            contract.PostSignHash = ComputePostSignHashForTest(contract);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contract);

            // 2. ACT
            var result = await _sut.VerifyContractIntegrityAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.IsTampered.Should().BeFalse();
            result.Data.OldPostSignHash.Should().Be(contract.PostSignHash);
            result.Data.NewPostSignHash.Should().Be(contract.PostSignHash);
            result.Data.IsMatched.Should().BeTrue();
        }

        [Fact]
        public async Task VerifyContractIntegrityAsync_CurrentPostSignHashDoesNotMatchStoredHash_ReturnsTamperedReport()
        {
            // 1. ARRANGE
            var contract = new Contract
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                Status = ContractStatusEnum.Active,
                PostSignHash = new string('2', 64),
                ContractVerification = new ContractVerification { IsLessorAgreed = true, IsLesseeAgreed = true },
                ContractSchedules = new List<ContractSchedule>()
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contract);

            // 2. ACT
            var result = await _sut.VerifyContractIntegrityAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.IsTampered.Should().BeTrue();
            result.Data.OldPostSignHash.Should().Be(contract.PostSignHash);
            result.Data.NewPostSignHash.Should().NotBe(contract.PostSignHash);
            result.Data.IsMatched.Should().BeFalse();
        }

        [Fact]
        public async Task ContractValidateOtpAsync_PreSignHashMismatch_ReturnsDraftAndClearsSigningSession()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            await _cache.SetStringAsync("OTP:SignContract:7:lessor-1", "123456");
            var contract = new Contract
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                Status = ContractStatusEnum.Signing,
                PreSignSnapshot = "{}",
                PreSignHash = new string('0', 64),
                ContractVerification = new ContractVerification(),
                ContractSchedules = new List<ContractSchedule>()
            };
            _mockUserRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()))
                .ReturnsAsync(new User { UserId = "lessor-1", Profile = new UserProfile { UserId = "lessor-1" } });
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contract);

            // 2. ACT
            var result = await _sut.ContractValidateOtpAsync(7, "123456");

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            contract.Status.Should().Be(ContractStatusEnum.Draft);
            contract.PreSignSnapshot.Should().BeNull();
            contract.PreSignHash.Should().BeNull();
            _mockContractRepository.Verify(r => r.UpdateAsync(contract), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ContractValidateOtpAsync_InvalidOtp_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            await _cache.SetStringAsync("OTP:SignContract:7:lessor-1", "123456");

            // 2. ACT
            var result = await _sut.ContractValidateOtpAsync(7, "000000");

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không hợp lệ");
        }

        private void SetupValidContractRequestValidation(ContractRequest request)
        {
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>()))
                .ReturnsAsync(new Space
                {
                    Id = request.SpaceId,
                    OwnerId = "lessor-1",
                    Owner = new User { UserId = "lessor-1", Name = "Lessor Name" }
                });
            _mockBookingRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<PrimaryBookingRequest, bool>>>(),
                    It.IsAny<Func<IQueryable<PrimaryBookingRequest>, IIncludableQueryable<PrimaryBookingRequest, object>>>()))
                .ReturnsAsync(new PrimaryBookingRequest
                {
                    Id = request.PrimaryBookingRequestId,
                    SpaceId = request.SpaceId,
                    LessorId = "lessor-1",
                    LesseeId = "lessee-1",
                    Lessee = new User { UserId = "lessee-1", Name = "Lessee Name" }
                });
            _mockConversationRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Conversation, bool>>>()))
                .ReturnsAsync(new Conversation { Id = request.ConversationId, LessorId = "lessor-1", LesseeId = "lessee-1" });
        }

        private void SetupParticipantProfiles()
        {
            var lessorProfile = new UserProfile
            {
                UserId = "lessor-1",
                IsVerified = true,
                IdentityCardNumber = "111",
                FullName = "Lessor Full Name",
                PermanentResidence = "Lessor Address",
                DateOfIssue = new DateOnly(2020, 1, 1)
            };
            var lesseeProfile = new UserProfile
            {
                UserId = "lessee-1",
                IsVerified = true,
                IdentityCardNumber = "222",
                FullName = "Lessee Full Name",
                PermanentResidence = "Lessee Address",
                DateOfIssue = new DateOnly(2021, 1, 1)
            };

            _mockProfileRepository
                .SetupSequence(r => r.GetAsync(
                    It.IsAny<Expression<Func<UserProfile, bool>>>()))
                .ReturnsAsync(lessorProfile)
                .ReturnsAsync(lesseeProfile);
        }

        private string ComputePostSignHashForTest(Contract contract)
        {
            var buildSnapshotMethod = typeof(ContractService).GetMethod("BuildPostSignSnapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var computeHashMethod = typeof(ContractService).GetMethod("ComputeSha256Hash", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var snapshot = (string)buildSnapshotMethod!.Invoke(_sut, new object[] { contract })!;
            return (string)computeHashMethod!.Invoke(null, new object[] { snapshot })!;
        }

        private static ContractRequest CreateContractRequest() =>
            new()
            {
                ConversationId = "conversation-1",
                SpaceId = 10,
                PrimaryBookingRequestId = 20,
                Duration = 1,
                DurationUnit = DurationUnitEnum.Months,
                StartDate = DateTime.Now.AddDays(1),
                Acreage = 50,
                Price = 1000,
                DepositAmount = 100,
                Description = "Contract",
                BusinessPurpose = "Office",
                ContractSchedules = new List<ContractScheduleRequest>
                {
                    new() { DayOfWeek = DayOfWeek.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) }
                }
            };
    }
}

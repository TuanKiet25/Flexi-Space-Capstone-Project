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
        private readonly Mock<IListingRepository> _mockListingRepository;
        private readonly Mock<ISpaceRepository> _mockSpaceRepository;
        private readonly Mock<ISpaceUsageRightRepository> _mockSpaceUsageRightRepository;
        private readonly Mock<IPrimaryBookingRequestRepository> _mockBookingRepository;
        private readonly Mock<IBannerRepository> _mockBannerRepository;
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
            _mockListingRepository = new Mock<IListingRepository>();
            _mockSpaceRepository = new Mock<ISpaceRepository>();
            _mockSpaceUsageRightRepository = new Mock<ISpaceUsageRightRepository>();
            _mockBookingRepository = new Mock<IPrimaryBookingRequestRepository>();
            _mockBannerRepository = new Mock<IBannerRepository>();
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
            _mockUnitOfWork.SetupGet(u => u.listingRepository).Returns(_mockListingRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.spaceRepository).Returns(_mockSpaceRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.spaceUsageRightRepository).Returns(_mockSpaceUsageRightRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.primaryBookingRequestRepository).Returns(_mockBookingRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.bannerRepository).Returns(_mockBannerRepository.Object);
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
        public async Task ShareContractAsync_CurrentUserIsNotLessor_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", Status = ContractStatusEnum.Draft });

            // 2. ACT
            var result = await _sut.ShareContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockMessageRepository.Verify(r => r.AddAsync(It.IsAny<Message>()), Times.Never);
        }

        [Fact]
        public async Task ShareContractAsync_NonDraftContract_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", Status = ContractStatusEnum.Signing });

            // 2. ACT
            var result = await _sut.ShareContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockMessageRepository.Verify(r => r.AddAsync(It.IsAny<Message>()), Times.Never);
        }

        [Fact]
        public async Task ShareContractAsync_DraftContractOwnedByCurrentUser_AddsProposalMessage()
        {
            // 1. ARRANGE
            var response = new MessageResponse { Id = "99", Content = "7" };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract
                {
                    Id = 7,
                    LessorId = "lessor-1",
                    Status = ContractStatusEnum.Draft,
                    ConversationId = "conversation-1"
                });
            _mockConversationRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Conversation, bool>>>()))
                .ReturnsAsync(new Conversation { Id = "conversation-1" });
            _mockMapper
                .Setup(m => m.Map<MessageResponse>(It.IsAny<Message>()))
                .Returns(response);

            // 2. ACT
            var result = await _sut.ShareContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
            _mockMessageRepository.Verify(r => r.AddAsync(It.Is<Message>(m =>
                m.ConversationId == "conversation-1" &&
                m.SenderId == "lessor-1" &&
                m.Content == "7" &&
                m.MessageType == MessageTypeEnum.ContractProposal)), Times.Once);
            _mockConversationRepository.Verify(r => r.UpdateAsync(It.IsAny<Conversation>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
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
        public async Task GetContractCalendarBySpaceAsync_CurrentUserIsListingCreator_ReturnsCalendarEntries()
        {
            // 1. ARRANGE
            var from = new DateTime(2026, 8, 3);
            var to = new DateTime(2026, 8, 3);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()))
                .ReturnsAsync(new Space { Id = 10, OwnerId = "lessor-1" });
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>() ))
                .ReturnsAsync(new Space { Id = 10, OwnerId = "lessor-1" });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>() ))
                .ReturnsAsync(new List<Listing> { new() { SpaceId = 10, CreatorId = "lessor-1" } });
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
        public async Task GetContractCalendarBySpaceAsync_ParentSpaceIncludesSpacePartCalendar()
        {
            // 1. ARRANGE
            var from = new DateTime(2026, 8, 3);
            var to = new DateTime(2026, 8, 3);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("listing-creator");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>() ))
                .ReturnsAsync(new Space
                {
                    Id = 10,
                    OwnerId = "space-owner",
                    ChildSpaces = new List<Space> { new() { Id = 11 } }
                });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>() ))
                .ReturnsAsync(new List<Listing> { new() { SpaceId = 11, CreatorId = "listing-creator" } });
            _mockContractRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>() ))
                .ReturnsAsync(new List<Contract>
                {
                    new()
                    {
                        Id = 9,
                        SpaceId = 11,
                        LesseeId = "lessee-1",
                        Status = ContractStatusEnum.Active,
                        StartDate = from,
                        EndDate = to,
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
            result.Data.Should().ContainSingle(e => e.ContractId == 9);
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
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>() ))
                .ReturnsAsync(new Space { Id = 10, OwnerId = "lessor-1" });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>() ))
                .ReturnsAsync(new List<Listing>());
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
        public async Task GetContractCalendarBySpaceAsync_CurrentUserIsOnlySpaceOwner_ReturnsForbidden()
        {
            // 1. ARRANGE
            var from = new DateTime(2026, 8, 3);
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("space-owner-only");
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>() ))
                .ReturnsAsync(new Space { Id = 10, OwnerId = "space-owner-only" });
            _mockSpaceRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Space, bool>>>(),
                    It.IsAny<Func<IQueryable<Space>, IIncludableQueryable<Space, object>>>() ))
                .ReturnsAsync(new Space { Id = 10, OwnerId = "space-owner-only" });
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>() ))
                .ReturnsAsync(new List<Listing> { new() { SpaceId = 10, CreatorId = "listing-creator" } });
            _mockContractRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>() ))
                .ReturnsAsync(new List<Contract>());

            // 2. ACT
            var result = await _sut.GetContractCalendarBySpaceAsync(10, from, from);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không có quyền");
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
        public async Task StartContractSigningAsync_ContractNotFound_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync((Contract)null!);

            // 2. ACT
            var result = await _sut.StartContractSigningAsync(7, new StartContractSigningRequest());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
        }

        [Fact]
        public async Task StartContractSigningAsync_CurrentUserIsNotLessor_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1", Status = ContractStatusEnum.Draft });

            // 2. ACT
            var result = await _sut.StartContractSigningAsync(7, new StartContractSigningRequest { LessorId = "lessor-1", LesseeId = "lessee-1" });

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task StartContractSigningAsync_ParticipantsMismatch_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1", Status = ContractStatusEnum.Draft });

            // 2. ACT
            var result = await _sut.StartContractSigningAsync(7, new StartContractSigningRequest { LessorId = "lessor-1", LesseeId = "other" });

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task StartContractSigningAsync_NonDraftContract_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1", Status = ContractStatusEnum.Active });

            // 2. ACT
            var result = await _sut.StartContractSigningAsync(7, new StartContractSigningRequest { LessorId = "lessor-1", LesseeId = "lessee-1" });

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task StartContractSigningAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ThrowsAsync(new InvalidOperationException("Start signing failure"));

            // 2. ACT
            var result = await _sut.StartContractSigningAsync(7, new StartContractSigningRequest());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Start signing failure");
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
        public async Task CancelContractSigningAsync_ContractNotFound_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync((Contract)null!);

            // 2. ACT
            var result = await _sut.CancelContractSigningAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
        }

        [Fact]
        public async Task CancelContractSigningAsync_CurrentUserIsNotLessor_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", Status = ContractStatusEnum.Signing });

            // 2. ACT
            var result = await _sut.CancelContractSigningAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task CancelContractSigningAsync_NonSigningContract_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", Status = ContractStatusEnum.Draft });

            // 2. ACT
            var result = await _sut.CancelContractSigningAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task CancelContractSigningAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ThrowsAsync(new InvalidOperationException("Cancel signing failure"));

            // 2. ACT
            var result = await _sut.CancelContractSigningAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Cancel signing failure");
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

        [Fact]
        public async Task ContractValidateOtpAsync_LastSignerActivatesContractAndOccupiesListing()
        {
            // 1. ARRANGE
            var listing = new Listing
            {
                Id = 50,
                Status = ListingStatusEnum.Available,
                IsActive = true,
                ListingType = ListingType.EntireSpace
            };
            var contract = new Contract
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                SpaceId = 10,
                PrimaryBookingRequestId = 20,
                ConversationId = "conversation-1",
                Status = ContractStatusEnum.Signing,
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 9, 1),
                CanShare = true,
                CanGrantSharePermission = true,
                ContractVerification = new ContractVerification { IsLessorAgreed = true },
                ContractSchedules = new List<ContractSchedule>(),
                PrimaryBookingRequest = new PrimaryBookingRequest { Id = 20, Listing = listing }
            };
            contract.PreSignSnapshot = BuildPreSignSnapshotForTest(contract);
            contract.PreSignHash = ComputeSha256HashForTest(contract.PreSignSnapshot);

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessee-1");
            _mockCurrentUserService.Setup(s => s.GetClientIpAddress()).Returns("127.0.0.1");
            await _cache.SetStringAsync("OTP:SignContract:7:lessee-1", "123456");
            _mockUserRepository
                .SetupSequence(r => r.GetAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()))
                .ReturnsAsync(new User
                {
                    UserId = "lessee-1",
                    Email = "lessee@example.com",
                    Profile = new UserProfile
                    {
                        UserId = "lessee-1",
                        FullName = "Lessee Full Name",
                        IdentityCardNumber = "222",
                        PermanentResidence = "Lessee Address",
                        DateOfIssue = new DateOnly(2021, 1, 1)
                    }
                })
                .ReturnsAsync(new User
                {
                    UserId = "lessor-1",
                    Email = "lessor@example.com",
                    Profile = new UserProfile { UserId = "lessor-1", FullName = "Lessor Full Name" }
                });
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contract);
            _mockSpaceUsageRightRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<SpaceUsageRight, bool>>>()))
                .ReturnsAsync((SpaceUsageRight)null!);
            _mockSpaceUsageRightRepository
                .Setup(r => r.AddAsync(It.IsAny<SpaceUsageRight>()))
                .Returns(Task.CompletedTask);
            _mockListingRepository
                .Setup(r => r.UpdateAsync(listing))
                .Returns(Task.CompletedTask);
            _mockMapper
                .Setup(m => m.Map<MessageResponse>((Message?)null))
                .Returns((MessageResponse)null!);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.ContractValidateOtpAsync(7, "123456");

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            contract.Status.Should().Be(ContractStatusEnum.Active);
            contract.IsActive.Should().BeTrue();
            contract.ContractSnapshot.Should().NotBeNullOrWhiteSpace();
            contract.PostSignHash.Should().NotBeNullOrWhiteSpace();
            contract.ContractVerification.IsLesseeAgreed.Should().BeTrue();
            contract.LesseeName.Should().Be("Lessee Full Name");
            listing.Status.Should().Be(ListingStatusEnum.Occupied);
            listing.IsActive.Should().BeFalse();
            listing.UpdatedBy.Should().Be("SystemContractSigning");
            _mockSpaceUsageRightRepository.Verify(r => r.AddAsync(It.Is<SpaceUsageRight>(x =>
                x.ContractId == 7 &&
                x.SpaceId == 10 &&
                x.UserId == "lessee-1" &&
                x.GrantedByUserId == "lessor-1" &&
                x.CanShare &&
                x.CanGrantSharePermission)), Times.Once);
            _mockListingRepository.Verify(r => r.UpdateAsync(listing), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateContractAsync_DraftContractOwnedByCurrentUser_UpdatesAndReturnsResponse()
        {
            // 1. ARRANGE
            var request = CreateContractRequest();
            var contract = new Contract
            {
                Id = 7,
                LessorId = "lessor-1",
                LesseeId = "lessee-1",
                Status = ContractStatusEnum.Draft,
                ContractSchedules = new List<ContractSchedule>(),
                Lessor = new User { UserId = "lessor-1", Name = "Old Lessor" },
                Lessee = new User { UserId = "lessee-1", Name = "Old Lessee" }
            };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync(contract);
            SetupValidContractRequestValidation(request);
            _mockMapper
                .Setup(m => m.Map(request, contract))
                .Callback(() =>
                {
                    contract.StartDate = request.StartDate;
                    contract.Duration = request.Duration;
                    contract.DurationUnit = request.DurationUnit;
                    contract.Price = request.Price;
                });
            _mockMapper
                .Setup(m => m.Map<ContractResponse>(contract))
                .Returns(() => new ContractResponse { Id = contract.Id, LessorId = contract.LessorId, LesseeId = contract.LesseeId });

            // 2. ACT
            var result = await _sut.UpdateContractAsync(7, request);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            contract.ContractSchedules.Should().HaveCount(1);
            contract.EndDate.Should().Be(request.StartDate.AddMonths(1));
            contract.Lessor!.Name.Should().Be("Lessor Name");
            contract.Lessee!.Name.Should().Be("Lessee Name");
            _mockContractRepository.Verify(r => r.UpdateAsync(contract), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateContractAsync_ContractNotFound_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ReturnsAsync((Contract)null!);

            // 2. ACT
            var result = await _sut.UpdateContractAsync(7, CreateContractRequest());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            _mockContractRepository.Verify(r => r.UpdateAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task DeleteContractAsync_DraftContractOwnedByCurrentUser_RemovesContract()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", Status = ContractStatusEnum.Draft });

            // 2. ACT
            var result = await _sut.DeleteContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteContractAsync_NonDraftContract_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", Status = ContractStatusEnum.Active });

            // 2. ACT
            var result = await _sut.DeleteContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockContractRepository.Verify(r => r.RemoveByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task SendContractOtpAsync_SigningContractParticipant_SavesOtpAndSendsEmail()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1", Status = ContractStatusEnum.Signing });
            _mockContractVerificationRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<ContractVerification, bool>>>(),
                    It.IsAny<Func<IQueryable<ContractVerification>, IIncludableQueryable<ContractVerification, object>>>()))
                .ReturnsAsync(new ContractVerification
                {
                    ContractId = 7,
                    Contract = new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1" }
                });
            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new User { UserId = "lessor-1", Email = "lessor@example.com" });

            // 2. ACT
            var result = await _sut.SendContractOtpAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            var savedOtp = await _cache.GetStringAsync("OTP:SignContract:7:lessor-1");
            savedOtp.Should().NotBeNullOrWhiteSpace();
            savedOtp.Should().HaveLength(6);
            _mockEmailService.Verify(s => s.SendContractOtpEmailAsync("lessor@example.com", It.IsAny<string>(), 7), Times.Once);
        }

        [Fact]
        public async Task SendContractOtpAsync_AlreadySigned_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1", Status = ContractStatusEnum.Signing });
            _mockContractVerificationRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<ContractVerification, bool>>>(),
                    It.IsAny<Func<IQueryable<ContractVerification>, IIncludableQueryable<ContractVerification, object>>>()))
                .ReturnsAsync(new ContractVerification
                {
                    ContractId = 7,
                    IsLessorAgreed = true,
                    Contract = new Contract { Id = 7, LessorId = "lessor-1", LesseeId = "lessee-1" }
                });

            // 2. ACT
            var result = await _sut.SendContractOtpAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            _mockEmailService.Verify(s => s.SendContractOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task DeactivateExpiredContractsAsync_NoExpiredContracts_ReturnsZero()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(new List<Contract>());

            // 2. ACT
            var result = await _sut.DeactivateExpiredContractsAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
            _mockContractRepository.Verify(r => r.UpdateAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task DeactivateExpiredContractsAsync_ExpiredContracts_MarksThemExpired()
        {
            // 1. ARRANGE
            var contracts = new List<Contract>
            {
                new() { Id = 7, Status = ContractStatusEnum.Active, IsActive = true, EndDate = DateTime.Now.AddDays(-1) },
                new() { Id = 8, Status = ContractStatusEnum.Active, IsActive = true, EndDate = DateTime.Now.AddDays(-2) }
            };
            _mockContractRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ReturnsAsync(contracts);

            // 2. ACT
            var result = await _sut.DeactivateExpiredContractsAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(2);
            contracts.Should().OnlyContain(c => c.Status == ContractStatusEnum.Expired && !c.IsActive && c.UpdatedBy == "SystemBackgroundWorker");
            _mockContractRepository.Verify(r => r.UpdateAsync(It.IsAny<Contract>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateContractAsync_RepositoryThrowsException_ReturnsFailedResultAndRollsBack()
        {
            // 1. ARRANGE
            var request = CreateContractRequest();
            SetupValidContractRequestValidation(request);
            SetupParticipantProfiles();
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("lessor-1");
            _mockMapper.Setup(m => m.Map<Contract>(request)).Returns(new Contract
            {
                SpaceId = request.SpaceId,
                PrimaryBookingRequestId = request.PrimaryBookingRequestId,
                ConversationId = request.ConversationId,
                StartDate = request.StartDate,
                Duration = request.Duration,
                DurationUnit = request.DurationUnit
            });
            _mockContractRepository
                .Setup(r => r.AddAsync(It.IsAny<Contract>()))
                .ThrowsAsync(new InvalidOperationException("Create contract failure"));

            // 2. ACT
            var result = await _sut.CreateContractAsync(request);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Create contract failure");
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ShareContractAsync_RepositoryThrowsException_ReturnsFailedResultAndRollsBack()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ThrowsAsync(new InvalidOperationException("Share failure"));

            // 2. ACT
            var result = await _sut.ShareContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Share failure");
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllContractsAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ThrowsAsync(new InvalidOperationException("Get contracts failure"));

            // 2. ACT
            var result = await _sut.GetAllContractsAsync(new FilterGetAllContract());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Get contracts failure");
        }

        [Fact]
        public async Task GetContractByIdAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ThrowsAsync(new InvalidOperationException("Get contract failure"));

            // 2. ACT
            var result = await _sut.GetContractByIdAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Get contract failure");
        }

        [Fact]
        public async Task UpdateContractAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Contract, bool>>>(),
                    It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()))
                .ThrowsAsync(new InvalidOperationException("Update contract failure"));

            // 2. ACT
            var result = await _sut.UpdateContractAsync(7, CreateContractRequest());

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Update contract failure");
        }

        [Fact]
        public async Task DeleteContractAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ThrowsAsync(new InvalidOperationException("Delete contract failure"));

            // 2. ACT
            var result = await _sut.DeleteContractAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Delete contract failure");
        }

        [Fact]
        public async Task DeactivateExpiredContractsAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockContractRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Contract, bool>>>()))
                .ThrowsAsync(new InvalidOperationException("Deactivate failure"));

            // 2. ACT
            var result = await _sut.DeactivateExpiredContractsAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Deactivate failure");
        }

        [Fact]
        public async Task ContractPrivateHelpers_RemainingBranches_ReturnExpectedResults()
        {
            // 1. ARRANGE
            var calculateEndDate = typeof(ContractService).GetMethod("CalculateEndDate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var populateProfiles = typeof(ContractService).GetMethod("PopulateContractParticipantProfilesAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var syncSchedules = typeof(ContractService).GetMethod("SyncContractSchedules", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var resetSigningSession = typeof(ContractService).GetMethod("ResetSigningSession", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var enrichResponse = typeof(ContractService).GetMethod("EnrichContractResponseForCurrentUser", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            var start = new DateTime(2026, 1, 1);
            var contractWithoutSchedules = new Contract();
            contractWithoutSchedules.ContractSchedules = null!;
            var contractWithSchedules = new Contract
            {
                ContractSchedules = new List<ContractSchedule>
                {
                    new() { DayOfWeek = DayOfWeek.Monday }
                }
            };
            var contractWithoutVerification = new Contract
            {
                PreSignSnapshot = "{}",
                PreSignHash = "hash",
                PostSignSnapshot = "{}",
                PostSignHash = "hash"
            };
            var contractMissingProfile = new Contract { LessorId = "lessor-1", LesseeId = "lessee-1" };
            _mockProfileRepository
                .SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<UserProfile, bool>>>()))
                .ReturnsAsync((UserProfile)null!)
                .ReturnsAsync(new UserProfile { UserId = "lessee-1", IsVerified = true });

            // 2. ACT
            var weekEnd = (DateTime)calculateEndDate!.Invoke(_sut, new object[] { start, DurationUnitEnum.Weeks, 2 })!;
            var dayEnd = (DateTime)calculateEndDate.Invoke(_sut, new object[] { start, DurationUnitEnum.Days, 3 })!;
            var yearEnd = (DateTime)calculateEndDate.Invoke(_sut, new object[] { start, DurationUnitEnum.Years, 1 })!;
            var defaultEnd = (DateTime)calculateEndDate.Invoke(_sut, new object[] { start, (DurationUnitEnum)999, 1 })!;
            var emptyCurrentUserResponse = new ContractResponse();
            var profileTask = (Task<string?>)populateProfiles!.Invoke(_sut, new object[] { contractMissingProfile })!;
            var profileError = await profileTask;
            enrichResponse!.Invoke(null, new object?[] { emptyCurrentUserResponse, new Contract { LessorId = "lessor-1", LesseeId = "lessee-1" }, " " });
            syncSchedules!.Invoke(_sut, new object?[] { contractWithoutSchedules, null });
            syncSchedules.Invoke(_sut, new object?[] { contractWithSchedules, new List<ContractScheduleRequest>() });
            resetSigningSession!.Invoke(null, new object[] { contractWithoutVerification });

            // 3. ASSERT
            weekEnd.Should().Be(start.AddDays(14));
            dayEnd.Should().Be(start.AddDays(3));
            yearEnd.Should().Be(start.AddYears(1));
            defaultEnd.Should().Be(start);
            emptyCurrentUserResponse.CurrentUserContractRole.Should().BeNull();
            profileError.Should().Be("Nguoi tham gia can cap nhat ho so CCCD truoc khi tao hop dong.");
            contractWithoutSchedules.ContractSchedules.Should().NotBeNull();
            contractWithoutSchedules.ContractSchedules.Should().BeEmpty();
            contractWithSchedules.ContractSchedules.Should().BeEmpty();
            contractWithoutVerification.PreSignSnapshot.Should().BeNull();
            contractWithoutVerification.PreSignHash.Should().BeNull();
            contractWithoutVerification.PostSignSnapshot.Should().BeNull();
            contractWithoutVerification.PostSignHash.Should().BeNull();
        }

        [Fact]
        public async Task ContractPrivateHelpers_UnverifiedProfiles_ReturnsVerificationError()
        {
            // 1. ARRANGE
            var populateProfiles = typeof(ContractService).GetMethod("PopulateContractParticipantProfilesAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            _mockProfileRepository
                .SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<UserProfile, bool>>>()))
                .ReturnsAsync(new UserProfile { UserId = "lessor-1", IsVerified = false })
                .ReturnsAsync(new UserProfile { UserId = "lessee-1", IsVerified = true });

            // 2. ACT
            var profileTask = (Task<string?>)populateProfiles!.Invoke(_sut, new object[] { new Contract { LessorId = "lessor-1", LesseeId = "lessee-1" } })!;
            var profileError = await profileTask;

            // 3. ASSERT
            profileError.Should().Be("Ca hai nguoi tham gia can xac thuc CCCD truoc khi tao hop dong.");
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
                    Lessor = new User { UserId = "lessor-1", Name = "Lessor Name" },
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

        private string BuildPreSignSnapshotForTest(Contract contract)
        {
            var buildSnapshotMethod = typeof(ContractService).GetMethod("BuildPreSignSnapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (string)buildSnapshotMethod!.Invoke(_sut, new object[] { contract })!;
        }

        private static string ComputeSha256HashForTest(string payload)
        {
            var computeHashMethod = typeof(ContractService).GetMethod("ComputeSha256Hash", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (string)computeHashMethod!.Invoke(null, new object[] { payload })!;
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

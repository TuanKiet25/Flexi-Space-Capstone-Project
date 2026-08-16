using AutoMapper;
using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;

namespace FlexiSpace.Application.Tests
{
    public class TransactionHistoryServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITransactionHistoryRepository> _mockTransactionHistoryRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly TransactionHistoryService _sut;

        public TransactionHistoryServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTransactionHistoryRepository = new Mock<ITransactionHistoryRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.SetupGet(u => u.transactionHistoryRepository).Returns(_mockTransactionHistoryRepository.Object);

            _sut = new TransactionHistoryService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task GetAllTransactionHistoryByUserId_UnauthenticatedUser_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns((string?)null);

            // 2. ACT
            var result = await _sut.GetAllTransactionHistoryByUserId();

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("User is not authenticated.");
            _mockTransactionHistoryRepository.Verify(r => r.GetAllAsync(
                It.IsAny<Expression<Func<TransactionHistory, bool>>>(),
                It.IsAny<Func<IQueryable<TransactionHistory>, IIncludableQueryable<TransactionHistory, object>>?>()), Times.Never);
        }

        [Fact]
        public async Task GetAllTransactionHistoryByUserId_AuthenticatedUser_ReturnsMappedHistories()
        {
            // 1. ARRANGE
            var histories = new List<TransactionHistory>
            {
                new() { Id = 1, Wallet = new Wallet { UserId = "user-1" }, IsDeleted = false }
            };
            var responses = new List<TransactionHistoryResponse>
            {
                new() { Id = 1, Description = "Deposit" }
            };

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockTransactionHistoryRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionHistory, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionHistory>, IIncludableQueryable<TransactionHistory, object>>?>()))
                .ReturnsAsync(histories);
            _mockMapper
                .Setup(m => m.Map<IEnumerable<TransactionHistoryResponse>>(histories))
                .Returns(responses);

            // 2. ACT
            var result = await _sut.GetAllTransactionHistoryByUserId();

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(responses);
        }

        [Fact]
        public async Task GetAllTransactionHistoryByUserId_RepositoryThrows_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockTransactionHistoryRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<TransactionHistory, bool>>>(),
                    It.IsAny<Func<IQueryable<TransactionHistory>, IIncludableQueryable<TransactionHistory, object>>?>()))
                .ThrowsAsync(new InvalidOperationException("db failed"));

            // 2. ACT
            var result = await _sut.GetAllTransactionHistoryByUserId();

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Error retrieving transaction history: db failed");
        }
    }
}

using AutoMapper;
using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class ConversationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IConversationRepository> _mockConversationRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ConversationService _sut;

        public ConversationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockConversationRepository = new Mock<IConversationRepository>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork.SetupGet(u => u.conversationRepository).Returns(_mockConversationRepository.Object);

            _sut = new ConversationService(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetOrCreateConversationAsync_ExistingConversation_ReturnsExistingId()
        {
            // 1. ARRANGE
            var conversation = new Conversation { Id = "conversation-1", LessorId = "lessor-1", LesseeId = "lessee-1" };
            _mockConversationRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Conversation, bool>>>()))
                .ReturnsAsync(conversation);

            // 2. ACT
            var result = await _sut.GetOrCreateConversationAsync("lessor-1", "lessee-1");

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(conversation.Id);
            result.Message.Should().Contain("đã tồn tại");
            _mockConversationRepository.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateConversationAsync_NewParticipants_CreatesConversation()
        {
            // 1. ARRANGE
            _mockConversationRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Conversation, bool>>>()))
                .ReturnsAsync((Conversation)null!);
            _mockConversationRepository
                .Setup(r => r.AddAsync(It.IsAny<Conversation>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.GetOrCreateConversationAsync("lessor-1", "lessee-1");

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrWhiteSpace();
            result.Message.Should().Contain("mới đã được tạo");
            _mockConversationRepository.Verify(r => r.AddAsync(It.Is<Conversation>(c =>
                c.LessorId == "lessor-1" && c.LesseeId == "lessee-1")), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetOrCreateConversationAsync_RepositoryThrowsException_ThrowsWrappedException()
        {
            // 1. ARRANGE
            _mockConversationRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Conversation, bool>>>()))
                .ThrowsAsync(new InvalidOperationException("Db failure"));

            // 2. ACT
            var act = async () => await _sut.GetOrCreateConversationAsync("lessor-1", "lessee-1");

            // 3. ASSERT
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*Lỗi khi lấy hoặc tạo cuộc trò chuyện: Db failure*");
        }

        [Fact]
        public async Task GetConversationsByUserIdAsync_RepositoryReturnsConversations_ReturnsMappedSuccessResult()
        {
            // 1. ARRANGE
            var conversations = new List<Conversation>
            {
                new() { Id = "conversation-1", LessorId = "user-1", LesseeId = "user-2" }
            };
            var response = new List<ConversationResp>
            {
                new() { Id = "conversation-1", LessorId = "user-1", LesseeId = "user-2" }
            };

            _mockConversationRepository
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Conversation, bool>>>(),
                    It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()))
                .ReturnsAsync(conversations);
            _mockMapper
                .Setup(m => m.Map<List<ConversationResp>>(conversations))
                .Returns(response);

            // 2. ACT
            var result = await _sut.GetConversationsByUserIdAsync("user-1");

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(response);
            result.Message.Should().Be("Lấy danh sách cuộc trò chuyện thành công.");
        }

        [Fact]
        public async Task GetConversationByParticipantsAsync_NotFound_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            _mockConversationRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Conversation, bool>>>(),
                    It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()))
                .ReturnsAsync((Conversation)null!);

            // 2. ACT
            var result = await _sut.GetConversationByParticipantsAsync("lessor-1", "lessee-1");

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Data.Should().BeNull();
            result.Message.Should().Contain("Không tìm thấy");
        }

        [Fact]
        public async Task GetConversationByParticipantsAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockConversationRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Conversation, bool>>>(),
                    It.IsAny<Func<IQueryable<Conversation>, IIncludableQueryable<Conversation, object>>>()))
                .ThrowsAsync(new InvalidOperationException("Db failure"));

            // 2. ACT
            var result = await _sut.GetConversationByParticipantsAsync("lessor-1", "lessee-1");

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Db failure");
        }
    }
}

using AutoMapper;
using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class MessageServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessageRepository> _mockMessageRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly MessageService _sut;

        public MessageServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageRepository = new Mock<IMessageRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork.SetupGet(u => u.messageRepository).Returns(_mockMessageRepository.Object);

            _sut = new MessageService(_mockUnitOfWork.Object, _mockCurrentUserService.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetMessagesAsync_RepositoryReturnsMessages_ReturnsMappedSuccessResult()
        {
            // 1. ARRANGE
            var messages = new List<Message>
            {
                new() { Id = "message-1", ConversationId = "conversation-1", SenderId = "user-1", Content = "Hello" }
            };
            var response = new List<MessageResponse>
            {
                new() { Id = "message-1", ConversationId = "conversation-1", SenderId = "user-1", Content = "Hello" }
            };

            _mockMessageRepository
                .Setup(r => r.GetMessagesAsync("conversation-1", null, 20))
                .ReturnsAsync(messages);
            _mockMapper
                .Setup(m => m.Map<List<MessageResponse>>(messages))
                .Returns(response);

            // 2. ACT
            var result = await _sut.GetMessagesAsync("conversation-1", null, 20);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task GetMessagesAsync_RepositoryThrowsException_ThrowsWrappedException()
        {
            // 1. ARRANGE
            _mockMessageRepository
                .Setup(r => r.GetMessagesAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Db failure"));

            // 2. ACT
            var act = async () => await _sut.GetMessagesAsync("conversation-1", null, 20);

            // 3. ASSERT
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*Lỗi khi lấy tin nhắn: Db failure*");
        }

        [Fact]
        public async Task SaveMessageAsync_ValidMessage_SavesAndReturnsMappedMessage()
        {
            // 1. ARRANGE
            var response = new MessageResponse
            {
                ConversationId = "conversation-1",
                SenderId = "user-1",
                Content = "Hello",
                MessageType = MessageTypeEnum.Text,
                IsRead = false
            };
            _mockMessageRepository
                .Setup(r => r.AddAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<MessageResponse>(It.IsAny<Message>()))
                .Returns(response);

            // 2. ACT
            var result = await _sut.SaveMessageAsync("conversation-1", "user-1", "Hello");

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(response);
            result.Message.Should().Contain("Tin nhắn đã được lưu");
            _mockMessageRepository.Verify(r => r.AddAsync(It.Is<Message>(m =>
                m.ConversationId == "conversation-1" &&
                m.SenderId == "user-1" &&
                m.Content == "Hello" &&
                m.MessageType == MessageTypeEnum.Text &&
                !m.IsRead)), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateMessagesToReadAsync_NoUnreadMessages_ReturnsFalse()
        {
            // 1. ARRANGE
            _mockMessageRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Message, bool>>>()))
                .ReturnsAsync(new List<Message>());

            // 2. ACT
            var result = await _sut.UpdateMessagesToReadAsync("conversation-1", "user-1");

            // 3. ASSERT
            result.Should().BeFalse();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateMessagesToReadAsync_UnreadMessagesExist_MarksMessagesReadAndSaves()
        {
            // 1. ARRANGE
            var messages = new List<Message>
            {
                new() { ConversationId = "conversation-1", SenderId = "user-2", IsRead = false },
                new() { ConversationId = "conversation-1", SenderId = "user-3", IsRead = false }
            };
            _mockMessageRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Message, bool>>>()))
                .ReturnsAsync(messages);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // 2. ACT
            var result = await _sut.UpdateMessagesToReadAsync("conversation-1", "user-1");

            // 3. ASSERT
            result.Should().BeTrue();
            messages.Should().OnlyContain(m => m.IsRead);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}

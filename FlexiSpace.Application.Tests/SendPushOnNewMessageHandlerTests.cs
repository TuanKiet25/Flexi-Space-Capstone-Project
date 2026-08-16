using FluentAssertions;
using FlexiSpace.Application.Events.Notification;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace FlexiSpace.Application.Tests
{
    public class SendPushOnNewMessageHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IConversationRepository> _mockConversationRepository;
        private readonly Mock<IDeviceTokenRepository> _mockDeviceTokenRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IExpoPushService> _mockExpoPushService;
        private readonly SendPushOnNewMessageHandler _sut;

        public SendPushOnNewMessageHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockConversationRepository = new Mock<IConversationRepository>();
            _mockDeviceTokenRepository = new Mock<IDeviceTokenRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockExpoPushService = new Mock<IExpoPushService>();

            _mockUnitOfWork.SetupGet(u => u.conversationRepository).Returns(_mockConversationRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.deviceTokenRepository).Returns(_mockDeviceTokenRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.userRepository).Returns(_mockUserRepository.Object);

            _sut = new SendPushOnNewMessageHandler(
                _mockUnitOfWork.Object,
                _mockExpoPushService.Object,
                Mock.Of<ILogger<SendPushOnNewMessageHandler>>());
        }

        [Fact]
        public async Task Handle_ConversationNotFound_DoesNotSendPush()
        {
            // 1. ARRANGE
            _mockConversationRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Conversation, bool>>>()))
                .ReturnsAsync((Conversation)null!);

            // 2. ACT
            await _sut.Handle(new ChatMessageReceivedEvent { ConversationId = "conv-1", SenderId = "sender-1" }, CancellationToken.None);

            // 3. ASSERT
            _mockExpoPushService.Verify(s => s.SendPushAsync(
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ReceiverHasTokens_SendsPushToReceiver()
        {
            // 1. ARRANGE
            var conversation = new Conversation { Id = "conv-1", LessorId = "lessor-1", LesseeId = "lessee-1" };
            var tokens = new List<string> { "ExpoPushToken" };

            _mockConversationRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Conversation, bool>>>()))
                .ReturnsAsync(conversation);
            _mockDeviceTokenRepository
                .Setup(r => r.GetTokensByUserIdAsync("lessee-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokens);
            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new User { UserId = "lessor-1", UserName = "Lessor Name" });
            _mockExpoPushService
                .Setup(s => s.SendPushAsync(tokens, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // 2. ACT
            await _sut.Handle(new ChatMessageReceivedEvent
            {
                ConversationId = "conv-1",
                SenderId = "lessor-1",
                Content = "Hello"
            }, CancellationToken.None);

            // 3. ASSERT
            _mockExpoPushService.Verify(s => s.SendPushAsync(
                tokens,
                "Tin nhắn mới từ Lessor Name",
                "Hello",
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ReceiverHasNoTokens_DoesNotSendPush()
        {
            // 1. ARRANGE
            _mockConversationRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Conversation, bool>>>()))
                .ReturnsAsync(new Conversation { Id = "conv-1", LessorId = "lessor-1", LesseeId = "lessee-1" });
            _mockDeviceTokenRepository
                .Setup(r => r.GetTokensByUserIdAsync("lessor-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());

            // 2. ACT
            await _sut.Handle(new ChatMessageReceivedEvent
            {
                ConversationId = "conv-1",
                SenderId = "lessee-1",
                Content = "Hello"
            }, CancellationToken.None);

            // 3. ASSERT
            _mockExpoPushService.Verify(s => s.SendPushAsync(
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DependencyThrows_SwallowsException()
        {
            // 1. ARRANGE
            _mockConversationRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Conversation, bool>>>()))
                .ThrowsAsync(new InvalidOperationException("db failed"));

            // 2. ACT
            var act = async () => await _sut.Handle(new ChatMessageReceivedEvent { ReceiverId = "receiver-1" }, CancellationToken.None);

            // 3. ASSERT
            await act.Should().NotThrowAsync();
        }
    }
}

using FlexiSpace.Application.IServices;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Events.Notification
{
    public class SendPushOnNewMessageHandler : INotificationHandler<ChatMessageReceivedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExpoPushService _expoPushService;
        private readonly ILogger<SendPushOnNewMessageHandler> _logger;

        public SendPushOnNewMessageHandler(
            IUnitOfWork unitOfWork,
            IExpoPushService expoPushService,
            ILogger<SendPushOnNewMessageHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _expoPushService = expoPushService;
            _logger = logger;
        }
        public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var conversation = await _unitOfWork.conversationRepository.GetAsync(c => c.Id == notification.ConversationId);
                if (conversation == null) return;

                var receiverId = (conversation.LessorId == notification.SenderId)
                                 ? conversation.LesseeId
                                 : conversation.LessorId;

                var tokens = await _unitOfWork.deviceTokenRepository.GetTokensByUserIdAsync(receiverId, cancellationToken);
                if (tokens.Any())
                {
                    var sender = await _unitOfWork.userRepository.GetAsync(s => s.UserId == notification.SenderId);
                    string senderName = sender?.UserName ?? "Người dùng";
                    string title = $"Tin nhắn mới từ {senderName}";
                    var customData = new { conversationId = notification.ConversationId, type = "CHAT" };

                    await _expoPushService.SendPushAsync(tokens, title, notification.Content!, customData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi Push Notification cho user {notification.ReceiverId}");
            }
        }
    }
}

using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        public MessageService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }
        public async Task<ServiceResult<List<MessageResponse>>> GetMessagesAsync(string conversationId, DateTime? timeBefore, int limit)
        {
            try
            {
                var results = await _unitOfWork.messageRepository.GetMessagesAsync(conversationId, timeBefore, limit);
                return new ServiceResult<List<MessageResponse>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<MessageResponse>>(results), 
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy tin nhắn: {ex.Message}");
            }
        }

        public async Task<ServiceResult<MessageResponse>> SaveMessageAsync(string conversationId,string senderId, string content)
        {
            try
            {
                var newMessage = new Message
                {
                    ConversationId = conversationId,
                    SenderId = senderId,
                    Content = content,
                    MessageType = MessageTypeEnum.Text,
                    CreateAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _unitOfWork.messageRepository.AddAsync(newMessage);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<MessageResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<MessageResponse>(newMessage),
                    Message = $"Tin nhắn đã được lưu với id: {newMessage.Id}"
                };
            }
            catch(Exception ex)
            {
                throw new Exception($"Lỗi khi lưu tin nhắn: {ex.Message}");
            }
        }
        public async Task<bool> UpdateMessagesToReadAsync(string conversationId, string currentUserId)
        {
            try
            {
                // 1. Truy vấn tìm các tin nhắn của đối phương gửi cho mình mà mình chưa đọc
                var unreadMessages = await _unitOfWork.messageRepository.GetAllAsync(
                    m => m.ConversationId == conversationId
                      && m.SenderId != currentUserId 
                      && m.IsRead == false
                // && !m.IsDeleted // (Tùy chọn) Bỏ comment dòng này nếu Entity Message của bạn có cờ IsDeleted
                );
                if (unreadMessages == null || !unreadMessages.Any())
                {
                    return false;
                }

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }

                // 3. Commit toàn bộ thay đổi xuống Database trong 1 lần duy nhất
                await _unitOfWork.SaveChangesAsync();
                // Trả về true để Hub biết là có thay đổi thực sự và cần bắn event SignalR
                return true;
            }
            catch (Exception )
            {
                return false;
            }
        }
    }
}

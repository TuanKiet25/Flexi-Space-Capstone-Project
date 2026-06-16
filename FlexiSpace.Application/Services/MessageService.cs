using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
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
        public MessageService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }
        public async Task<ServiceResult<List<Message>>> GetMessagesAsync(string conversationId, DateTime? timeBefore, int limit)
        {
            try
            {
                var results = await _unitOfWork.messageRepository.GetMessagesAsync(conversationId, timeBefore, limit);
                return new ServiceResult<List<Message>>
                {
                    IsSuccess = true,
                    Data = results
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy tin nhắn: {ex.Message}");
            }
        }

        public async Task<ServiceResult<string>> SaveMessageAsync(string conversationId,string senderId, string content)
        {
            try
            {
                var newMessage = new Message
                {
                    ConversationId = conversationId,
                    SenderId = senderId,
                    Content = content,
                    CreateAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _unitOfWork.messageRepository.AddAsync(newMessage);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<string>
                {
                    IsSuccess = true,
                    Data = newMessage.Id,
                    Message = $"Tin nhắn đã được lưu với id: {newMessage.Id}"
                };
            }
            catch(Exception ex)
            {
                throw new Exception($"Lỗi khi lưu tin nhắn: {ex.Message}");
            }
        }
    }
}

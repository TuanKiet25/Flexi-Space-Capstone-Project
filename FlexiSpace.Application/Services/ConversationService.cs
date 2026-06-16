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
    public class ConversationService : IConversationService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ConversationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ServiceResult<string>> GetOrCreateConversationAsync(string lessorId, string lesseeId)
        {
            try
            {
                var existingConversation = await _unitOfWork.conversationRepository.GetAsync(c => (c.LessorId == lessorId && c.LesseeId == lesseeId) || (c.LessorId == lesseeId && c.LesseeId == lessorId));
                if (existingConversation != null)
                {
                    return new ServiceResult<string>
                    {
                        IsSuccess = true,
                        Data = existingConversation.Id,
                        Message = $"Cuộc trò chuyện đã tồn tại. id: {existingConversation.Id}"
                    };
                }
                var newConversation = new Conversation
                {
                    LessorId = lessorId,
                    LesseeId = lesseeId,
                    LastMessage = DateTime.UtcNow
                };
                await _unitOfWork.conversationRepository.AddAsync(newConversation);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<string>
                {
                    IsSuccess = true,
                    Data = newConversation.Id,
                    Message = $"Cuộc trò chuyện mới đã được tạo. id: {newConversation.Id}"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy hoặc tạo cuộc trò chuyện: {ex.Message}");
            }
        }
    }
}

using AutoMapper;
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
        private readonly IMapper _mapper;
        public ConversationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        public async Task<ServiceResult<List<ConversationResp>>> GetConversationsByUserIdAsync(string userId)
        {
            try
            {
                var conversations = await _unitOfWork.conversationRepository.GetAllAsync(c => c.LessorId == userId || c.LesseeId == userId);

                return new ServiceResult<List<ConversationResp>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<ConversationResp>>(conversations),
                    Message = "Lấy danh sách cuộc trò chuyện thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<ConversationResp>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ConversationResp>> GetConversationByParticipantsAsync(string lessorId, string lesseeId)
        {
            try
            {
                var conversation = await _unitOfWork.conversationRepository.GetAsync(c =>
                    (c.LessorId == lessorId && c.LesseeId == lesseeId) ||
                    (c.LessorId == lesseeId && c.LesseeId == lessorId));

                if (conversation == null)
                {
                    return new ServiceResult<ConversationResp>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy cuộc trò chuyện giữa 2 người dùng này."
                    };
                }

                return new ServiceResult<ConversationResp>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ConversationResp>(conversation),
                    Message = "Lấy cuộc trò chuyện thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ConversationResp>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
    }
}

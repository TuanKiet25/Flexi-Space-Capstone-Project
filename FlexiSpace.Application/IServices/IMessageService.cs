using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IMessageService
    {
        Task<ServiceResult<MessageResponse>> SaveMessageAsync(string conversationId, string senderId, string content);
        Task<ServiceResult<List<MessageResponse>>> GetMessagesAsync(string conversationId, DateTime? timeBefore, int limit);
        Task<bool> UpdateMessagesToReadAsync(string conversationId, string currentUserId);
    }
}

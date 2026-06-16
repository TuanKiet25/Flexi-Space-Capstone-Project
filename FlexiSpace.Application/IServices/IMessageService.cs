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
        Task<ServiceResult<string>> SaveMessageAsync(string conversationId, string senderId, string content);
        Task<ServiceResult<List<Message>>> GetMessagesAsync(string conversationId, DateTime? timeBefore, int limit);
    }
}

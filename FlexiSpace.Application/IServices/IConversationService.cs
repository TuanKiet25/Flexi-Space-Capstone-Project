using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IConversationService
    {
        Task<ServiceResult<string>> GetOrCreateConversationAsync(string lessorId, string lesseeId);
        Task<ServiceResult<List<ConversationResp>>> GetConversationsByUserIdAsync(string userId);
        Task<ServiceResult<ConversationResp>> GetConversationByParticipantsAsync(string lessorId, string lesseeId);
    }
}

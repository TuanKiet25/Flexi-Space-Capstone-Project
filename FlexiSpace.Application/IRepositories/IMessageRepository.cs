using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Application.IRepositories
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<List<Message>> GetMessagesAsync(string conversationId, DateTime? timeBefore, int limit);
    }
}

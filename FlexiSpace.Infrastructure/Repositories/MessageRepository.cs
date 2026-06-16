using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext context) : base(context)
        {
        }
            public async Task<List<Message>> GetMessagesAsync(string conversationId, DateTime? timeBefore, int limit)
            {
                var query = _context.Messages
                .Where(m => m.ConversationId == conversationId); 

                if (timeBefore.HasValue)
                {
                    query = query.Where(m => m.CreateAt < timeBefore.Value);
                }
                 var messages = await query
                    .OrderByDescending(m => m.CreateAt)
                    .Take(limit)
                    .ToListAsync();
                messages.Reverse();
                return messages;
             }
    }
}

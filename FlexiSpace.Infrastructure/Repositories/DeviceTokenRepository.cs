using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class DeviceTokenRepository : GenericRepository<DeviceToken>, IDeviceTokenRepository
    {
        public DeviceTokenRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<string>> GetTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.DeviceTokens  
        .Where(t => t.UserId == userId)
        .Select(t => t.ExpoPushToken)
        .ToListAsync(cancellationToken);
        }
    }
}

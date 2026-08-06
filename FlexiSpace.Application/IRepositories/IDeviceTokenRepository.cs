using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Application.IRepositories
{
    public interface IDeviceTokenRepository : IGenericRepository<DeviceToken>
    {
        Task<List<string>> GetTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    }
}

using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class UserAiImageHistoryRepository : GenericRepository<UserAiImageHistory>, IUserAiImageHistoryRepository
    {
        public UserAiImageHistoryRepository(AppDbContext context) : base(context)
        {
        }
    }
}

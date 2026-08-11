using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class SpaceUsageRightRepository : GenericRepository<SpaceUsageRight>, ISpaceUsageRightRepository
    {
        public SpaceUsageRightRepository(AppDbContext context) : base(context)
        {
        }
    }
}

using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class SpaceAllowedCategoryRepository : GenericRepository<SpaceAllowedCategory>, ISpaceAllowedCategoryRepository
    {
        public SpaceAllowedCategoryRepository(AppDbContext context) : base(context)
        {
        }
    }
}

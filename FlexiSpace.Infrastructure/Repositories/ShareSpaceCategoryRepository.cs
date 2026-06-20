using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class ShareSpaceCategoryRepository : GenericRepository<ShareSpaceCategory>, IShareSpaceCategoryRepository
    {
        public ShareSpaceCategoryRepository(AppDbContext context) : base(context)
        {
        }
    }
}

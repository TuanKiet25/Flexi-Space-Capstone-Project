using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class ShareSpaceDetailRepository : GenericRepository<ShareSpaceDetail>, IShareSpaceDetailRepository
    {
        public ShareSpaceDetailRepository(AppDbContext context) : base(context)
        {
        }
    }
}

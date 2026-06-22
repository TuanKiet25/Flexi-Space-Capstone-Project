using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class SharedSpaceAmenitiesRepository : GenericRepository<SharedSpaceAmenities>, ISharedSpaceAmenitiesRepository
    {
        public SharedSpaceAmenitiesRepository(AppDbContext context) : base(context)
        {
        }
    }
}

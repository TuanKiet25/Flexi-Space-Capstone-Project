using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class AvailabilitiesTimeRepository : GenericRepository<AvailabilitiesTime>, IAvailabilitiesTimeRepository
    {
        public AvailabilitiesTimeRepository(AppDbContext context) : base(context)
        {
        }
    }
}

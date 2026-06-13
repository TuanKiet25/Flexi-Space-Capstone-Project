using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class PrimaryBookingRequestRepository : GenericRepository<PrimaryBookingRequest>, IPrimaryBookingRequestRepository
    {
        public PrimaryBookingRequestRepository(AppDbContext context) : base(context)
        {
        }
    }
}

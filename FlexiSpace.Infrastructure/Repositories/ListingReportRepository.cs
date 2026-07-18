using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class ListingReportRepository : GenericRepository<ListingReport>, IListingReportRepository
    {
        public ListingReportRepository(AppDbContext context) : base(context)
        {
        }
    }
}

using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class ListingViewDailyStatRepository : GenericRepository<ListingViewDailyStat>, IListingViewDailyStatRepository
    {
        public ListingViewDailyStatRepository(AppDbContext context) : base(context)
        {
        }
    }
}

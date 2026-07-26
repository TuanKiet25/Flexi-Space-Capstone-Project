using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class FavoriteListRepository : GenericRepository<FavoriteList>, IFavoriteListRepository
    {
        public FavoriteListRepository(AppDbContext context) : base(context)
        {
        }
    }
}

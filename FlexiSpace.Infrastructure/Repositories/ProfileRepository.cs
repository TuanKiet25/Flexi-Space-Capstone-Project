using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class ProfileRepository : GenericRepository<UserProfile>, IProfileRepository
    {
        public ProfileRepository(AppDbContext context) : base(context)
        {
        }
    }
}

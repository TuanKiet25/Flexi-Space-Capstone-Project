using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class UserOTPRepository : GenericRepository<UserOTP>, IUserOTPRepository
    {
        public UserOTPRepository(AppDbContext context) : base(context)
        {
        }
    }
}

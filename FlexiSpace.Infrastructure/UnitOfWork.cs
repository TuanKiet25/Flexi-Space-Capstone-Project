using FlexiSpace.Application;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Infrastructure.Repositories;

namespace FlexiSpace.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;
        public IUserRepository userRepository { get; }
        public IUserOTPRepository userOTPRepository { get; }
        public ISpaceRepository spaceRepository {  get; }
        public ISpaceAmenityRepository spaceAmenityRepository { get; }
        public ISpaceAllowedCategoryRepository spaceAllowedCategoryRepository { get; }

        public UnitOfWork(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            userRepository = new UserRepository(_dbContext);
            userOTPRepository = new UserOTPRepository(_dbContext);
            spaceRepository = new SpaceRepository(_dbContext);
            spaceAmenityRepository = new SpaceAmenityRepository(_dbContext);
            spaceAllowedCategoryRepository = new SpaceAllowedCategoryRepository(_dbContext);
        }



        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}

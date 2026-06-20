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
        public ISpaceAllowedCategoryRepository spaceAllowedCategoryRepository { get; }
        public IBussinessCategoryRepository bussinessCategoryRepository { get; }
        public IAmentityRepository amenityRepository { get; }
        public IListingRepository listingRepository { get; }
        public IContractRepository contractRepository { get; }
        public IPrimaryBookingRequestRepository primaryBookingRequestRepository { get; }
        public IConversationRepository conversationRepository { get; }
        public IMessageRepository messageRepository { get; }

        public UnitOfWork(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            userRepository = new UserRepository(_dbContext);
            userOTPRepository = new UserOTPRepository(_dbContext);
            spaceRepository = new SpaceRepository(_dbContext);
            spaceAllowedCategoryRepository = new SpaceAllowedCategoryRepository(_dbContext);
            listingRepository = new ListingRepository(_dbContext);
            contractRepository = new ContractRepository(_dbContext);
            bussinessCategoryRepository = new BussinessCategoryRepository(_dbContext);
            amenityRepository = new AmentityRepository(_dbContext);
            primaryBookingRequestRepository = new PrimaryBookingRequestRepository(_dbContext);
            conversationRepository = new ConversationRepository(_dbContext);
            messageRepository = new MessageRepository(_dbContext);
        }



        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}

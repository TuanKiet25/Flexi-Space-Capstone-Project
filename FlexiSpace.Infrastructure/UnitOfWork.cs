using FlexiSpace.Application;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlexiSpace.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;
        private IDbContextTransaction? _transaction;
        public IUserRepository userRepository { get; }
        public IUserOTPRepository userOTPRepository { get; }
        public ISpaceRepository spaceRepository {  get; }
        public ISpaceAllowedCategoryRepository spaceAllowedCategoryRepository { get; }
        public IBussinessCategoryRepository bussinessCategoryRepository { get; }
        public IAmentityRepository amenityRepository { get; }
        public IListingRepository listingRepository { get; }
        public IContractRepository contractRepository { get; }
        public IContractVerificationRepository contractVerificationRepository { get; }
        public IPrimaryBookingRequestRepository primaryBookingRequestRepository { get; }
        public IConversationRepository conversationRepository { get; }
        public IMessageRepository messageRepository { get; }
        public IShareSpaceDetailRepository shareSpaceDetailRepository { get; }
        public IShareSpaceCategoryRepository shareSpaceCategoryRepository { get; }
        public IAvailabilitiesTimeRepository availabilitiesTimeRepository { get; }
        public ISharedSpaceAmenitiesRepository sharedSpaceAmenitiesRepository { get; }
        public IProfileRepository profileRepository { get; }
        public ITransactionRepository transactionRepository { get; }
        public IWalletRepository walletRepository { get; }

        public UnitOfWork(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _transaction = null;
            userRepository = new UserRepository(_dbContext);
            userOTPRepository = new UserOTPRepository(_dbContext);
            spaceRepository = new SpaceRepository(_dbContext);
            spaceAllowedCategoryRepository = new SpaceAllowedCategoryRepository(_dbContext);
            listingRepository = new ListingRepository(_dbContext);
            contractRepository = new ContractRepository(_dbContext);
            contractVerificationRepository = new ContractVerificationRepository(_dbContext);
            bussinessCategoryRepository = new BussinessCategoryRepository(_dbContext);
            amenityRepository = new AmentityRepository(_dbContext);
            primaryBookingRequestRepository = new PrimaryBookingRequestRepository(_dbContext);
            conversationRepository = new ConversationRepository(_dbContext);
            messageRepository = new MessageRepository(_dbContext);
            shareSpaceDetailRepository = new ShareSpaceDetailRepository(_dbContext);
            shareSpaceCategoryRepository = new ShareSpaceCategoryRepository(_dbContext);
            availabilitiesTimeRepository = new AvailabilitiesTimeRepository(_dbContext);
            sharedSpaceAmenitiesRepository = new SharedSpaceAmenitiesRepository(_dbContext);
            profileRepository = new ProfileRepository(_dbContext);
            transactionRepository = new TransactionRepository(_dbContext);
            walletRepository = new WalletRepository(_dbContext);
        }



        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }

            _transaction = await _dbContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            finally
            {
     
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                }
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }
        public void Dispose()
        {
            _transaction?.Dispose();
            _dbContext.Dispose();
        }
    }
}

using FlexiSpace.Application.IRepositories;

namespace FlexiSpace.Application
{
    public interface IUnitOfWork
    {
        IUserRepository userRepository { get; }
        IUserOTPRepository userOTPRepository { get; }
        ISpaceRepository spaceRepository { get; }
        ISpaceAllowedCategoryRepository spaceAllowedCategoryRepository { get; }
        IBussinessCategoryRepository bussinessCategoryRepository { get; }
        IAmentityRepository amenityRepository { get; }
        IListingRepository listingRepository { get; }
        IContractRepository contractRepository { get; }
        IContractVerificationRepository contractVerificationRepository { get; }
        IPrimaryBookingRequestRepository primaryBookingRequestRepository { get; }
        IConversationRepository conversationRepository { get; }
        IMessageRepository messageRepository { get; }
        IShareSpaceDetailRepository shareSpaceDetailRepository { get; }
        IShareSpaceCategoryRepository shareSpaceCategoryRepository { get; }
        IAvailabilitiesTimeRepository availabilitiesTimeRepository { get; }
        ISharedSpaceAmenitiesRepository sharedSpaceAmenitiesRepository { get; }
        IProfileRepository profileRepository { get; }
        ITransactionRepository transactionRepository { get; }
        IWalletRepository walletRepository { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}

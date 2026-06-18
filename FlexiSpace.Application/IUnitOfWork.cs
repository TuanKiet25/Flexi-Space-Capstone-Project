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
        IPrimaryBookingRequestRepository primaryBookingRequestRepository { get; }
        IConversationRepository conversationRepository { get; }
        IMessageRepository messageRepository { get; }
        Task<int> SaveChangesAsync();

    }
}

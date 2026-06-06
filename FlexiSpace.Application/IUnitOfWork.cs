using FlexiSpace.Application.IRepositories;

namespace FlexiSpace.Application
{
    public interface IUnitOfWork
    {
        IUserRepository userRepository { get; }
        IUserOTPRepository userOTPRepository { get; }
        ISpaceAmenityRepository spaceAmenityRepository { get; }
        ISpaceRepository spaceRepository { get; }
        ISpaceAllowedCategoryRepository spaceAllowedCategoryRepository { get; }
        IListingRepository listingRepository { get; }
        Task<int> SaveChangesAsync();

    }
}

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
        Task<int> SaveChangesAsync();

    }
}

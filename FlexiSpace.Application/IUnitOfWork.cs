using FlexiSpace.Application.IRepositories;

namespace FlexiSpace.Application
{
    public interface IUnitOfWork
    {
        IUserRepository userRepository { get; }
        IUserOTPRepository userOTPRepository { get; }
        Task<int> SaveChangesAsync();

    }
}

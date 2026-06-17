using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IDataSeederService
    {
        Task SeedAdminAccountAsync();
    }
}
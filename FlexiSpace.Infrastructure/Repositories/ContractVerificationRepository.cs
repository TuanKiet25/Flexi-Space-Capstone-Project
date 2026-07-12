using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class ContractVerificationRepository : GenericRepository<ContractVerification>, IContractVerificationRepository
    {
        public ContractVerificationRepository(AppDbContext context) : base(context)
        {
        }
    }
}

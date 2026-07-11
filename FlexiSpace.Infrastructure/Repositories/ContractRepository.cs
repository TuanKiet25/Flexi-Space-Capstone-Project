using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class ContractRepository : GenericRepository<Contract>, IContractRepository
    {
        public ContractRepository(AppDbContext context) : base(context)
        {
        }

    }
}
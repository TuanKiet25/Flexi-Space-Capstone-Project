using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class PriorityLevelRepository : GenericRepository<PriorityLevel>, IPriorityLevelRepository
    {
        public PriorityLevelRepository(AppDbContext context) : base(context)
        {
        }
    }
}

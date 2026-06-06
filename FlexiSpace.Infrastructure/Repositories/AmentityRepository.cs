using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class AmentityRepository : GenericRepository<Amentity>, IAmentityRepository
    {
        public AmentityRepository(AppDbContext context) : base(context)
        {
        }
    }
}

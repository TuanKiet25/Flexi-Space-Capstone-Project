using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Helper
{
    public interface IInsertAndUpdate<P, C> where P : BaseEntity where C : BaseEntity
    {
        Task<ServiceResult<P>> Insert(P parentEntity, List<C> childEntities);
        //Task<ServiceResult<P>> Update(P parentEntity, IEnumerable<C> tobeAdded, IEnumerable<C> tobeUpdated);
    }
}

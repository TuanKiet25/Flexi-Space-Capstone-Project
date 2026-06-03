using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Helper
{
    public class InsertAndUpdate<P, C> : IInsertAndUpdate<P, C> where P : BaseEntity where C : BaseEntity
    {
        private readonly AppDbContext _dbContext;
        private readonly DbSet<P> _parentEntity;
        private readonly DbSet<C> _childEntity;

        public InsertAndUpdate(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _parentEntity = _dbContext.Set<P>();
            _childEntity = _dbContext.Set<C>();
        }

        public async Task<ServiceResult<P>> Insert(P parentEntity, List<C> childEntities)
        {
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                var createDate = DateTime.Now;
                var createBy = GlobalVariables.CurrentUserId;
                parentEntity.CreatedAt = createDate;
                parentEntity.CreatedBy = createBy;
                _parentEntity.Add(parentEntity);
                childEntities.ForEach(c =>
                {
                    c.CreatedAt = createDate;
                    c.CreatedBy = createBy;
                });
                await _childEntity.AddRangeAsync(childEntities);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return (new ServiceResult<P>
                {
                    IsSuccess = true,
                    Data = parentEntity
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (new ServiceResult<P>
                {
                    IsSuccess = false,
                    Message = ex.Message
                });
            }
        }

        public Task<ServiceResult<P>> Update(P parentEntity, IEnumerable<C> tobeAdded, IEnumerable<C> tobeUpdated)
        {
            throw new NotImplementedException();
        }
    }
}

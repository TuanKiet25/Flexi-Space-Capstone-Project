using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Application.IServices;
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
        private readonly ICurrentUserService _currentUserService;

        public InsertAndUpdate(AppDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _parentEntity = _dbContext.Set<P>();
            _childEntity = _dbContext.Set<C>();
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<P>> Insert(P parentEntity, List<C> childEntities)
        {
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                var createDate = DateTime.Now;
                var createBy = _currentUserService.UserId ?? "System";
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
                return new ServiceResult<P>
                {
                    IsSuccess = true,
                    Data = parentEntity
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ServiceResult<P>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        //public async Task<ServiceResult<P>> Update(P parentEntity, IEnumerable<C> tobeAdded, IEnumerable<C> tobeUpdated)
        //{
        //    using var transaction = _dbContext.Database.BeginTransaction();
        //    try
        //    {
        //        var updateDate = DateTime.Now;
        //        var updateBy = GlobalVariables.CurrentUserId;
        //        parentEntity.UpdatedAt = updateDate;
        //        parentEntity.UpdatedBy = updateBy;
        //        _dbContext.Entry(parentEntity).CurrentValues.SetValues(parentEntity);
        //        _dbContext.Entry(parentEntity).Property(e => e.CreatedAt).IsModified = false;
        //        _dbContext.Entry(parentEntity).Property(e => e.CreatedBy).IsModified = false;
        //        tobeAdded.ToList().ForEach(c =>
        //        {
        //            c.CreatedAt = updateDate;
        //            c.CreatedBy = updateBy;
        //        });
        //        await _childEntity.AddRangeAsync(tobeAdded);
        //        foreach (var c in tobeUpdated)
        //        {
        //            var existing = await _childEntity.FirstOrDefaultAsync(x => x.Id == c.Id);
        //            if (existing != null)
        //            {
        //                _dbContext.Entry(existing).CurrentValues.SetValues(c);
        //                existing.UpdatedAt = updateDate;
        //                existing.UpdatedBy = updateBy;
        //            }
        //        }
        //        _childEntity.UpdateRange(tobeUpdated);
        //        await _dbContext.SaveChangesAsync();
        //        await transaction.CommitAsync();
        //        return new ServiceResult<P>
        //        {
        //            IsSuccess = true,
        //            Data = parentEntity
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        return new ServiceResult<P>
        //        {
        //            IsSuccess = false,
        //            Message = ex.Message
        //        };
        //    }
        //}
    }
}

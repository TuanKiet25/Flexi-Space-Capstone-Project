using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IRepositories
{
    public interface IGenericRepository<T> where T : class
    {
        //Task<int> SaveChangesAsync();
        Task<T> GetAsync(Expression<Func<T, bool>> filter);

        Task AddAsync(T entity);

        Task RemoveByIdAsync(object id);

        Task RemoveRangeAsync(IEnumerable<T> entities);

        Task<int> CountAsync();

        Task AddRangeAsync(List<T> entities);

        Task AddRangeAsync(ICollection<T> entities);

        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter);

        Task<T> GetAsync(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include);

        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter,
                                               Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, int pageIndex = 1, int pageSize = 25);

        Task<List<T>> GetAllAsync(System.Linq.Expressions.Expression<Func<T, bool>>? filter,
                                                   Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null);

        Task UpdateAsync(T entity);

        Task<T> GetByIdAsync(Guid id);
        Task DeleteAsync(Guid id);
        void RemoveRange(IEnumerable<T> entities);
    }

}

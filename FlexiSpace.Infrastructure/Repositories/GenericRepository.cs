
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        public readonly DbSet<T> _db;
        public readonly AppDbContext _context;


        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _db = _context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            await _db.AddAsync(entity);
        }

        public async Task<int> CountAsync() => await _db.CountAsync();

        public async Task<List<T>> GetAllAsync(System.Linq.Expressions.Expression<Func<T, bool>>? filter,
                                               Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
                                               int pageIndex = 1,
                                               int pageSize = 5)
        {
            IQueryable<T> query = _db;


            if (filter != null)
            {
                query = query.Where(filter);
            }

            query.IgnoreQueryFilters();

            if (include != null)
            {
                query = include(query);
            }
            return await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<T>> GetAllAsync(System.Linq.Expressions.Expression<Func<T, bool>>? filter,
                                               Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
        {
            IQueryable<T> query = _db;


            if (filter != null)
            {
                query = query.Where(filter);
            }


            if (include != null)
            {
                query = include(query);
            }
            return await query
                .ToListAsync();
        }


        public async Task<List<T>> GetAllAsync(System.Linq.Expressions.Expression<Func<T, bool>>? filter)
        {
            if (filter != null)
            {
                return await _db.Where(filter).ToListAsync();
            }
            return await _db.ToListAsync();
        }

        public async Task<T> GetAsync(System.Linq.Expressions.Expression<Func<T, bool>> filter)
        {
#nullable disable
            IQueryable<T> query = _db;
            return await query.FirstOrDefaultAsync(filter);
#nullable restore
        }

        public async Task<T> GetAsync(System.Linq.Expressions.Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
        {

            IQueryable<T> query = _db;
            
            // Apply include FIRST
            if (include != null)
            {
                query = include(query);
            }
            
            // Then apply filter
            if (filter != null)
            {
                query = query.Where(filter);
            }

#pragma warning disable CS8603 // Possible null reference return.
            return await query.FirstOrDefaultAsync();
#pragma warning restore CS8603 // Possible null reference return.

        }

        public async Task RemoveByIdAsync(object id)
        {
#nullable disable
            T existing = await _db.FindAsync(id);
#nullable restore
            if (existing != null)
            {
                _db.Remove(existing);
            }
            else throw new Exception();
        }

        public async Task AddRangeAsync(List<T> entities)
        {
            await _db.AddRangeAsync(entities);
        }

        public async Task AddRangeAsync(ICollection<T> entities)
        {
            await _db.AddRangeAsync(entities);
        }

        public async Task UpdateAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _db.Update(entity);
        }

        public async Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (!entityList.Any()) return;

            _db.RemoveRange(entityList);
            // await _context.SaveChangesAsync(); // <-- XÓA DÒNG NÀY
            await Task.CompletedTask; // (Hoặc làm cho hàm này đồng bộ)
        }

        public async Task<T> GetByIdAsync(Guid id)
        {

#pragma warning disable CS8603 // Possible null reference return.
            return await _db.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
#pragma warning restore CS8603 // Possible null reference return.
        }
        public async Task DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity with ID {id} not found.");
            }
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted= true;
                baseEntity.UpdatedAt = DateTime.UtcNow;
                _db.Update(entity);
            }
            else
            {
                _db.Remove(entity);
            }
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _db.RemoveRange(entities);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

}

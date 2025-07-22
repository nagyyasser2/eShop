using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using eShop.Core.Services.Abstractions;

namespace eShop.Core.Services.Implementations
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly DbContext _context;
        private readonly DbSet<T> _dbSet;

        public BaseRepository(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
        }

        // Read Operations
        public T? GetById(int id, string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return query.FirstOrDefault(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task<T?> GetByIdAsync(int id, string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public IEnumerable<T> GetAll(string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return query.ToList();
        }

        public async Task<IEnumerable<T>> GetAllAsync(string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return await query.ToListAsync();
        }

        public IEnumerable<T> GetAllPaged(int skip, int take, string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return query.Skip(skip).Take(take).ToList();
        }

        public async Task<IEnumerable<T>> GetAllPagedAsync(int skip, int take, string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return await query.Skip(skip).Take(take).ToListAsync();
        }

        public T? Find(Expression<Func<T, bool>> match, string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return query.FirstOrDefault(match);
        }

        public async Task<T?> FindAsync(Expression<Func<T, bool>> match, string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return await query.FirstOrDefaultAsync(match);
        }

        public IEnumerable<T>? FindAll(Expression<Func<T, bool>> match, string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return query.Where(match).ToList();
        }

        public async Task<IEnumerable<T>?> FindAllAsync(Expression<Func<T, bool>> match, string[]? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return await query.Where(match).ToListAsync();
        }

        public bool Any(Expression<Func<T, bool>> match)
        {
            return _dbSet.Any(match);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> match)
        {
            return await _dbSet.AnyAsync(match);
        }

        public int Count(Expression<Func<T, bool>>? match = null)
        {
            return match == null ? _dbSet.Count() : _dbSet.Count(match);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? match = null)
        {
            return match == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(match);
        }

        // Create Operations
        public T Add(T entity)
        {
            _dbSet.Add(entity);
            return entity;
        }

        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public IEnumerable<T> AddRange(IEnumerable<T> entities)
        {
            _dbSet.AddRange(entities);
            return entities;
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        // Update Operations
        public T Update(T entity)
        {
            _dbSet.Update(entity);
            return entity;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return entity;
        }

        public IEnumerable<T> UpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
            return entities;
        }

        public async Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
            return entities;
        }

        // Delete Operations
        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task RemoveAsync(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void RemoveById(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public async Task RemoveByIdAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public async Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        // Save Changes
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
using System.Linq.Expressions;

namespace eShop.Core.Services.Abstractions
{
    public interface IBaseRepository<T> where T : class
    {
        // Read Operations
        T? GetById(int id, string[]? includes = null);
        Task<T?> GetByIdAsync(int id, string[]? includes = null);
        IEnumerable<T> GetAll(string[]? includes = null);
        Task<IEnumerable<T>> GetAllAsync(string[]? includes = null);
        IEnumerable<T> GetAllPaged(int skip, int take, string[]? includes = null);
        Task<IEnumerable<T>> GetAllPagedAsync(int skip, int take, string[]? includes = null);
        IEnumerable<T> GetFilteredPaged(Expression<Func<T, bool>> match, int skip, int take, string[]? includes = null);
        Task<IEnumerable<T>> GetFilteredPagedAsync(Expression<Func<T, bool>> match, int skip, int take, string[]? includes = null);
        T? Find(Expression<Func<T, bool>> match, string[]? includes = null);
        Task<T?> FindAsync(Expression<Func<T, bool>> match, string[]? includes = null);
        IEnumerable<T>? FindAll(Expression<Func<T, bool>> match, string[]? includes = null);
        Task<IEnumerable<T>?> FindAllAsync(Expression<Func<T, bool>> match, string[]? includes = null);
        bool Any(Expression<Func<T, bool>> match);
        Task<bool> AnyAsync(Expression<Func<T, bool>> match);
        int Count(Expression<Func<T, bool>>? match = null);
        Task<int> CountAsync(Expression<Func<T, bool>>? match = null);

        // Create Operations
        T Add(T entity);
        Task<T> AddAsync(T entity);
        IEnumerable<T> AddRange(IEnumerable<T> entities);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

        // Update Operations
        T Update(T entity);
        Task<T> UpdateAsync(T entity);
        IEnumerable<T> UpdateRange(IEnumerable<T> entities);
        Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);

        // Delete Operations
        void Remove(T entity);
        Task RemoveAsync(T entity);
        void RemoveById(int id);
        Task RemoveByIdAsync(int id);
        void RemoveRange(IEnumerable<T> entities);
        Task RemoveRangeAsync(IEnumerable<T> entities);

        // Save Changes
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
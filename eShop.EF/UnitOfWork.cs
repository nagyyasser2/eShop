using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using eShop.EF;

namespace eShop.Core.Services.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private readonly IBaseRepository<Product> _productRepository;
        private readonly IBaseRepository<Order> _orderRepository;
        private readonly IBaseRepository<Category> _categoryRepository;
        private readonly IBaseRepository<Banner> _bannerRepository;

        private bool _disposed = false;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _productRepository = new BaseRepository<Product>(context);
            _orderRepository = new BaseRepository<Order>(context);
            _categoryRepository = new BaseRepository<Category>(context);
            _bannerRepository = new BaseRepository<Banner>(context);
        }

        public IBaseRepository<Product> ProductRepository => _productRepository;
        public IBaseRepository<Order> OrderRepository => _orderRepository;
        public IBaseRepository<Category> CategoryRepository => _categoryRepository;
        public IBaseRepository<Banner> BannerRepository => _bannerRepository;
        public IDbContextTransaction BeginTransaction()
        {
            return _context.Database.BeginTransaction();
        }

        public async Task CommitTransactionAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

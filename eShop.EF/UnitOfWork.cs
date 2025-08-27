using Microsoft.EntityFrameworkCore.Storage;
using eShop.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using eShop.EF.Repositories;
using eShop.Core.Models;
using eShop.EF;

namespace eShop.Core.Services.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IBaseRepository<OrderItem> _orderItemRepository;
        private readonly IBaseRepository<Product> _productRepository;
        private readonly IBaseRepository<Variant> _variantRepository;
        private readonly IBaseRepository<Payment> _paymentRepository;
        private readonly IBaseRepository<Banner> _bannerRepository;
        private readonly IBaseRepository<Order> _orderRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBaseRepository<Image> _imageRepository;
        private readonly DbContext _context;

        private bool _disposed = false;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _orderItemRepository = new BaseRepository<OrderItem>(context);
            _productRepository = new BaseRepository<Product>(context);
            _variantRepository = new BaseRepository<Variant>(context);
            _paymentRepository = new BaseRepository<Payment>(context);
            _bannerRepository = new BaseRepository<Banner>(context);
            _categoryRepository = new CategoriesRepository(context);
            _orderRepository = new BaseRepository<Order>(context);
            _imageRepository = new BaseRepository<Image>(context);
        }

        public IBaseRepository<OrderItem> OrderItemRepository => _orderItemRepository;
        public IBaseRepository<Variant> VariantRepository => _variantRepository;
        public IBaseRepository<Product> ProductRepository => _productRepository;
        public IBaseRepository<Payment> PaymentRepository => _paymentRepository;
        public IBaseRepository<Banner> BannerRepository => _bannerRepository;
        public ICategoryRepository CategoryRepository => _categoryRepository;
        public IBaseRepository<Order> OrderRepository => _orderRepository;
        public IBaseRepository<Image> ImageRepository => _imageRepository;

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

using eShop.Core.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace eShop.Core.Services.Abstractions
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository CategoryRepository { get; }
        IBaseRepository<Product> ProductRepository { get; }
        IBaseRepository<Order> OrderRepository { get; }
        IBaseRepository<Banner> BannerRepository { get; }
        IBaseRepository<Variant> VariantRepository { get; }
        IBaseRepository<Image> ImageRepository { get; }
        IBaseRepository<ProductImage> ProductImageRepository { get; }
        IBaseRepository<Payment> PaymentRepository { get; }
        IBaseRepository<OrderItem> OrderItemRepository { get; }

        IDbContextTransaction BeginTransaction();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
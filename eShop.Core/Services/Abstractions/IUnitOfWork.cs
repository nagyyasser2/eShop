using eShop.Core.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace eShop.Core.Services.Abstractions
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<Category> CategoryRepository { get; }
        IBaseRepository<Product> ProductRepository { get; }
        IBaseRepository<Order> OrderRepository { get; }
        IBaseRepository<Banner> BannerRepository { get; } 
        IBaseRepository<Variant> VariantRepository { get; }
        IBaseRepository<Image> ImageRepository { get; }
        IDbContextTransaction BeginTransaction();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}

using System.Linq.Expressions;
using eShop.Core.DTOs;
using eShop.Core.Models;

namespace eShop.Core.Services.Abstractions
{
    public interface IProductService
    {
        // Basic CRUD Operations
        Task<ProductDTO?> GetProductByIdAsync(int id);
        Task<ProductDTO> CreateProductAsync(CreateProductDto productDto);
        Task<ProductDTO?> UpdateProductAsync(UpdateProductDto productDto);
        Task<bool> DeleteProductAsync(int id);

        // Specialized Query Operations
        Task<(IEnumerable<ProductDTO> Products, int TotalCount)> GetFilteredPagedAsync(
            Expression<Func<Product, bool>> filter,
            int skip,
            int take,
            string[]? includes = null);

        // Status Management Operations
        Task<bool> ToggleProductStatusAsync(int id);
        Task<bool> ToggleFeaturedStatusAsync(int id);

        // Stock Management Operations
        Task<bool> UpdateStockQuantityAsync(int id, int quantity);

        // Validation Operations
        Task<bool> ProductExistsAsync(int id);
        Task<bool> ProductExistsBySKUAsync(string sku, int? excludeProductId = null);
    }
}
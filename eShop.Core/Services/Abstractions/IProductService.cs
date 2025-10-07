using System.Linq.Expressions;
using eShop.Core.Models;
using eShop.Core.DTOs.Products;

namespace eShop.Core.Services.Abstractions
{
    public interface IProductService
    {
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto?> CreateProductAsync(CreateProductRequest createProductDto);
        Task<ProductDto?> UpdateProductAsync(UpdateProductRequest productDto);
        Task<bool> DeleteProductAsync(int id);
        Task<(IEnumerable<ProductDto> Products, int TotalCount)> GetFilteredPagedAsync(
            Expression<Func<Product, bool>> filter,
            int skip,
            int take,
            string[]? includes = null);
        Task<bool> ToggleProductStatusAsync(int id);
        Task<bool> ToggleFeaturedStatusAsync(int id);
        Task<bool> UpdateStockQuantityAsync(int id, int quantity);
        Task<bool> ProductExistsAsync(int id);
        Task<bool> ProductExistsBySKUAsync(string sku, int? excludeProductId = null);
    }
}
using eShop.Core.DTOs.Products;
using System.Linq.Expressions;
using eShop.Core.Models;

namespace eShop.Core.Services.Abstractions
{
    public interface IProductService
    {
        Task<ProductDto?> GetProductByIdAsync(int id);

        Task<(IEnumerable<ProductDto> Products, int TotalCount)> GetFilteredPagedAsync(
            int skip,
            int take,
            bool? featured = null,
            bool? active = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? daysBack = null,
            string[]? tags = null,
            string[]? includes = null);

        Task<ProductDto?> CreateProductAsync(CreateProductRequest createProductRequest);

        Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest updateProductRequest);

        Task DeleteProductAsync(int id);

        Task<bool> ToggleProductStatusAsync(int id);

        Task<bool> ToggleFeaturedStatusAsync(int id);

        Task<bool> UpdateStockQuantityAsync(int id, int quantity);

        Task<bool> ProductExistsAsync(int id);

        Task<bool> ProductExistsBySKUAsync(string sku, int? excludeProductId = null);
    }
}
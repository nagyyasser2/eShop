using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.Services.Abstractions
{
    public interface IProductService
    {
        // Basic CRUD Operations
        Task<ProductDTO?> GetProductByIdAsync(int id);
        Task<IEnumerable<CreateProductDto>> GetAllProductsAsync();
        Task<CreateProductDto> CreateProductAsync(CreateProductDto productDto);
        Task<CreateProductDto?> UpdateProductAsync(CreateProductDto productDto);
        Task<bool> DeleteProductAsync(int id);

        // Specialized Query Operations
        Task<IEnumerable<CreateProductDto>> GetActiveProductsAsync();
        Task<IEnumerable<CreateProductDto>> GetFeaturedProductsAsync();
        Task<IEnumerable<CreateProductDto>> GetProductsByCategoryAsync(int categoryId);

        // Status Management Operations
        Task<bool> ToggleProductStatusAsync(int id);
        Task<bool> ToggleFeaturedStatusAsync(int id);

        // Stock Management Operations
        Task<bool> UpdateStockQuantityAsync(int id, int quantity);

        // Validation Operations
        Task<bool> ProductExistsAsync(int id);
        Task<bool> ProductExistsBySKUAsync(string sku, int? excludeProductId = null);

        // Additional Query Operations
        //Task<IEnumerable<ProductDTO>> SearchProductsAsync(string searchTerm);
        //Task<IEnumerable<ProductDTO>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        //Task<IEnumerable<ProductDTO>> GetLowStockProductsAsync(int threshold = 10);
        //Task<IEnumerable<ProductDTO>> GetProductsByTagsAsync(string tags);

        // Pagination Support
        //Task<(IEnumerable<ProductDTO> products, int totalCount)> GetProductsPagedAsync(
            //int pageNumber,
            //int pageSize,
            //string? searchTerm = null,
            //int? categoryId = null,
            //bool? isActive = null,
            //bool? isFeatured = null,
            //string? sortBy = null,
            //bool sortDescending = false);

        // Bulk Operations
        //Task<bool> BulkUpdateStatusAsync(IEnumerable<int> productIds, bool isActive);
        //Task<bool> BulkUpdateFeaturedAsync(IEnumerable<int> productIds, bool isFeatured);
        //Task<bool> BulkDeleteAsync(IEnumerable<int> productIds);

        //// Statistics and Analytics
        //Task<int> GetTotalProductCountAsync();
        //Task<int> GetActiveProductCountAsync();
        //Task<int> GetFeaturedProductCountAsync();
        //Task<decimal> GetAveragePriceAsync();
        //Task<ProductDTO?> GetMostExpensiveProductAsync();
        //Task<ProductDTO?> GetCheapestProductAsync();
    }
}

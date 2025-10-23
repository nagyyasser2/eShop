using eShop.Core.Services.Abstractions;

namespace eShop.Core.Services.Implementations
{
    /// <summary>
    /// Centralized helper for cache invalidation across services.
    /// Ensures consistent cache invalidation patterns and relationships.
    /// </summary>
    public class CacheInvalidationHelper(ICacheService cacheService)
    {
        // Cache prefixes - centralized for consistency
        private const string ProductDetailPrefix = "products:detail:";
        private const string ProductListPrefix = "products:list:";
        private const string CategoryListPrefix = "categories:list:";
        private const string CategoryDetailPrefix = "categories:detail:";

        /// <summary>
        /// Invalidates all product-related caches (both detail and list views).
        /// </summary>
        public async Task InvalidateAllProductCachesAsync()
        {
            await cacheService.InvalidateMultiplePatternsAsync(
                ProductDetailPrefix,
                ProductListPrefix
            );
        }

        /// <summary>
        /// Invalidates a specific product's detail cache and all product lists.
        /// </summary>
        public async Task InvalidateProductAsync(int productId)
        {
            await cacheService.InvalidateAsync($"{ProductDetailPrefix}{productId}");
            await cacheService.InvalidateByPatternAsync(ProductListPrefix);
        }

        /// <summary>
        /// Invalidates all product list caches (used when filters might be affected).
        /// </summary>
        public async Task InvalidateProductListsAsync()
        {
            await cacheService.InvalidateByPatternAsync(ProductListPrefix);
        }

        /// <summary>
        /// Invalidates all category-related caches (both detail and list views).
        /// </summary>
        public async Task InvalidateAllCategoryCachesAsync()
        {
            await cacheService.InvalidateMultiplePatternsAsync(
                CategoryDetailPrefix,
                CategoryListPrefix
            );
        }

        /// <summary>
        /// Invalidates a specific category and all category lists.
        /// </summary>
        public async Task InvalidateCategoryAsync(int categoryId)
        {
            await cacheService.InvalidateAsync($"{CategoryDetailPrefix}{categoryId}");
            await cacheService.InvalidateByPatternAsync(CategoryListPrefix);
        }

        /// <summary>
        /// Invalidates caches when a category is modified.
        /// This invalidates:
        /// - The specific category caches
        /// - All category list caches
        /// - All product caches (since products display category info and filtering by category)
        /// </summary>
        public async Task InvalidateCategoryWithRelatedProductsAsync(int categoryId)
        {
            await cacheService.InvalidateMultiplePatternsAsync(
                $"{CategoryDetailPrefix}{categoryId}",
                CategoryListPrefix,
                ProductDetailPrefix,
                ProductListPrefix
            );
        }

        /// <summary>
        /// Invalidates caches when multiple categories are affected.
        /// Useful for bulk operations.
        /// </summary>
        public async Task InvalidateMultipleCategoriesWithProductsAsync(params int[] categoryIds)
        {
            var patterns = new List<string>
            {
                CategoryListPrefix,
                ProductDetailPrefix,
                ProductListPrefix
            };

            // Add specific category detail keys
            patterns.AddRange(categoryIds.Select(id => $"{CategoryDetailPrefix}{id}"));

            await cacheService.InvalidateMultiplePatternsAsync(patterns.ToArray());
        }

        /// <summary>
        /// Invalidates only category caches (useful when change doesn't affect products).
        /// </summary>
        public async Task InvalidateCategoryOnlyAsync(int categoryId)
        {
            await cacheService.InvalidateAsync($"{CategoryDetailPrefix}{categoryId}");
            await cacheService.InvalidateByPatternAsync(CategoryListPrefix);
        }
    }
}
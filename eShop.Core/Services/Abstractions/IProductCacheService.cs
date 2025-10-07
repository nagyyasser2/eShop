using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.Services.Abstractions
{
    public interface IProductCacheService
    {
        /// <summary>
        /// Retrieves a cached value by key
        /// </summary>
        /// <typeparam name="T">Type of cached object</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached value or default if not found</returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Stores a value in cache
        /// </summary>
        /// <typeparam name="T">Type of object to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="expiration">Optional expiration time (default: 5 minutes)</param>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Invalidates cache for a specific product detail
        /// </summary>
        /// <param name="productId">Product ID</param>
        Task InvalidateProductAsync(int productId);

        /// <summary>
        /// Invalidates all product list caches
        /// </summary>
        Task InvalidateProductListAsync();
    }
}

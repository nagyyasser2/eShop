using eShop.Core.Services.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace eShop.Core.Services.Implementations
{
    public class ProductCacheService : IProductCacheService
    {
        private readonly IDistributedCache _cache;

        // Cache key constants
        private const string PRODUCT_LIST_CACHE_PREFIX = "products:list:";
        private const string PRODUCT_DETAIL_CACHE_PREFIX = "products:detail:";
        private const string PRODUCT_CACHE_KEYS_SET = "products:cache-keys";

        public ProductCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key);

                if (string.IsNullOrEmpty(cachedData))
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(cachedData);
            }
            catch (Exception)
            {
                // Log the exception in production
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var serializedData = JsonSerializer.Serialize(value);

                var options = new DistributedCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    // Default expiration: 5 minutes
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                }

                await _cache.SetStringAsync(key, serializedData, options);

                // Track list cache keys for bulk invalidation
                if (key.StartsWith(PRODUCT_LIST_CACHE_PREFIX))
                {
                    await TrackCacheKeyAsync(key);
                }
            }
            catch (Exception)
            {
                // Log the exception in production
                // Don't throw - caching should fail gracefully
            }
        }

        public async Task InvalidateProductAsync(int productId)
        {
            try
            {
                var cacheKey = $"{PRODUCT_DETAIL_CACHE_PREFIX}{productId}";
                await _cache.RemoveAsync(cacheKey);
            }
            catch (Exception)
            {
                // Log the exception in production
            }
        }

        public async Task InvalidateProductListAsync()
        {
            try
            {
                // Get all tracked cache keys
                var keysData = await _cache.GetStringAsync(PRODUCT_CACHE_KEYS_SET);

                if (!string.IsNullOrEmpty(keysData))
                {
                    var keys = JsonSerializer.Deserialize<HashSet<string>>(keysData);

                    if (keys != null && keys.Any())
                    {
                        // Remove all list cache entries
                        var tasks = keys.Select(key => _cache.RemoveAsync(key));
                        await Task.WhenAll(tasks);
                    }
                }

                // Clear the tracking set
                await _cache.RemoveAsync(PRODUCT_CACHE_KEYS_SET);
            }
            catch (Exception)
            {
                // Log the exception in production
            }
        }

        private async Task TrackCacheKeyAsync(string cacheKey)
        {
            try
            {
                var keysData = await _cache.GetStringAsync(PRODUCT_CACHE_KEYS_SET);

                var keys = string.IsNullOrEmpty(keysData)
                    ? new HashSet<string>()
                    : JsonSerializer.Deserialize<HashSet<string>>(keysData) ?? new HashSet<string>();

                keys.Add(cacheKey);

                var serializedKeys = JsonSerializer.Serialize(keys);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };

                await _cache.SetStringAsync(PRODUCT_CACHE_KEYS_SET, serializedKeys, options);
            }
            catch (Exception)
            {
                // Log the exception in production
                // Don't throw - tracking is not critical
            }
        }
    }
}
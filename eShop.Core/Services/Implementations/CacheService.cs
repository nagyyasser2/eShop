using Microsoft.Extensions.Caching.Distributed;
using eShop.Core.Services.Abstractions;
using System.Text.Json;

namespace eShop.Core.Services.Implementations
{
    public class CacheService(IDistributedCache cache) : ICacheService
    {
        private const string CACHE_KEYS_SET = "cache:tracked-keys";

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var cachedData = await cache.GetStringAsync(key);

                if (string.IsNullOrEmpty(cachedData))
                    return default;

                return JsonSerializer.Deserialize<T>(cachedData);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var serializedData = JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
                };

                await cache.SetStringAsync(key, serializedData, options);

                await TrackCacheKeyAsync(key);
            }
            catch (Exception)
            {
            }
        }

        public async Task InvalidateAsync(string key)
        {
            try
            {
                await cache.RemoveAsync(key);
                await RemoveTrackedKeyAsync(key);
            }
            catch (Exception)
            {
            }
        }

        public async Task InvalidateByPatternAsync(string pattern)
        {
            try
            {
                var keysData = await cache.GetStringAsync(CACHE_KEYS_SET);

                if (string.IsNullOrEmpty(keysData))
                    return;

                var keys = JsonSerializer.Deserialize<HashSet<string>>(keysData) ?? new HashSet<string>();

                var matchingKeys = keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();

                var tasks = matchingKeys.Select(k => cache.RemoveAsync(k));
                await Task.WhenAll(tasks);

                foreach (var k in matchingKeys)
                    keys.Remove(k);

                await UpdateTrackedKeysAsync(keys);
            }
            catch (Exception)
            {
            }
        }

        public async Task InvalidateMultiplePatternsAsync(params string[] patterns)
        {
            try
            {
                if (patterns == null || patterns.Length == 0)
                    return;

                var keysData = await cache.GetStringAsync(CACHE_KEYS_SET);

                if (string.IsNullOrEmpty(keysData))
                    return;

                var keys = JsonSerializer.Deserialize<HashSet<string>>(keysData) ?? new HashSet<string>();

                var matchingKeys = keys.Where(k =>
                    patterns.Any(pattern => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (!matchingKeys.Any())
                    return;

                var tasks = matchingKeys.Select(k => cache.RemoveAsync(k));
                await Task.WhenAll(tasks);

                foreach (var k in matchingKeys)
                    keys.Remove(k);

                await UpdateTrackedKeysAsync(keys);
            }
            catch (Exception)
            {
            }
        }

        private async Task TrackCacheKeyAsync(string cacheKey)
        {
            try
            {
                var keysData = await cache.GetStringAsync(CACHE_KEYS_SET);
                var keys = string.IsNullOrEmpty(keysData)
                    ? new HashSet<string>()
                    : JsonSerializer.Deserialize<HashSet<string>>(keysData) ?? new HashSet<string>();

                keys.Add(cacheKey);

                await UpdateTrackedKeysAsync(keys);
            }
            catch (Exception)
            {
            }
        }

        private async Task RemoveTrackedKeyAsync(string cacheKey)
        {
            try
            {
                var keysData = await cache.GetStringAsync(CACHE_KEYS_SET);
                if (string.IsNullOrEmpty(keysData)) return;

                var keys = JsonSerializer.Deserialize<HashSet<string>>(keysData) ?? new HashSet<string>();
                if (keys.Remove(cacheKey))
                {
                    await UpdateTrackedKeysAsync(keys);
                }
            }
            catch (Exception)
            {
            }
        }

        private async Task UpdateTrackedKeysAsync(HashSet<string> keys)
        {
            var serializedKeys = JsonSerializer.Serialize(keys);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };

            await cache.SetStringAsync(CACHE_KEYS_SET, serializedKeys, options);
        }
    }
}
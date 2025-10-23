using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.Services.Abstractions
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task InvalidateAsync(string key);
        Task InvalidateByPatternAsync(string pattern);
        Task InvalidateMultiplePatternsAsync(params string[] patterns);
    }
}

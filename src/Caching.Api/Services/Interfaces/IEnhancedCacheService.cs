using Caching.Api.Models;

namespace Caching.Api.Services.Interfaces;

public interface IEnhancedCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, CachePolicy policy);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task ClearAsync();
    Task<IEnumerable<string>> GetKeysByTagAsync(string tag);
    Task RemoveByTagAsync(string tag);
}

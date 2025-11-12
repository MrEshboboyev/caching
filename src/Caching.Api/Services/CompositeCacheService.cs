using Caching.Api.Models;
using Caching.Api.Services.Interfaces;

namespace Caching.Api.Services;

public class CompositeCacheService(
    ILogger<CompositeCacheService> logger, 
    params IEnhancedCacheService[] cacheLayers
) : IEnhancedCacheService
{
    private readonly IList<IEnhancedCacheService> _cacheLayers = [.. cacheLayers];

    public async Task<T?> GetAsync<T>(string key)
    {
        // Try each cache layer in order
        foreach (var cacheLayer in _cacheLayers)
        {
            try
            {
                var result = await cacheLayer.GetAsync<T>(key);
                if (result != null)
                {
                    logger.LogInformation("Retrieved data from cache layer: {CacheLayer}", cacheLayer.GetType().Name);
                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving from cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
        }

        logger.LogInformation("Data not found in any cache layer for key: {Key}", key);
        return default(T);
    }

    public async Task SetAsync<T>(string key, T value, CachePolicy policy)
    {
        // Set in all cache layers
        foreach (var cacheLayer in _cacheLayers)
        {
            try
            {
                await cacheLayer.SetAsync(key, value, policy);
                logger.LogInformation("Set data in cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting data in cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
        }
    }

    public async Task RemoveAsync(string key)
    {
        // Remove from all cache layers
        foreach (var cacheLayer in _cacheLayers)
        {
            try
            {
                await cacheLayer.RemoveAsync(key);
                logger.LogInformation("Removed data from cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing data from cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        // Check if exists in any cache layer
        foreach (var cacheLayer in _cacheLayers)
        {
            try
            {
                if (await cacheLayer.ExistsAsync(key))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking existence in cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
        }

        return false;
    }

    public async Task ClearAsync()
    {
        // Clear all cache layers
        foreach (var cacheLayer in _cacheLayers)
        {
            try
            {
                await cacheLayer.ClearAsync();
                logger.LogInformation("Cleared cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
        }
    }

    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag)
    {
        var allKeys = new HashSet<string>();

        // Get keys from all cache layers
        foreach (var cacheLayer in _cacheLayers)
        {
            try
            {
                var keys = await cacheLayer.GetKeysByTagAsync(tag);
                foreach (var key in keys)
                {
                    allKeys.Add(key);
                }
                logger.LogInformation("Retrieved keys by tag from cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving keys by tag from cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
        }

        return allKeys;
    }

    public async Task RemoveByTagAsync(string tag)
    {
        // Remove by tag from all cache layers
        foreach (var cacheLayer in _cacheLayers)
        {
            try
            {
                await cacheLayer.RemoveByTagAsync(tag);
                logger.LogInformation("Removed keys by tag from cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing keys by tag from cache layer: {CacheLayer}", cacheLayer.GetType().Name);
            }
        }
    }
}

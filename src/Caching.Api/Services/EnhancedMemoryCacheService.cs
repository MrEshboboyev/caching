using Caching.Api.Models;
using Caching.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Caching.Api.Services;

public class EnhancedMemoryCacheService(
    IMemoryCache memoryCache,
    ICacheMetricsService metricsService,
    ILogger<EnhancedMemoryCacheService> logger
) : IEnhancedCacheService
{
    private readonly Dictionary<string, List<string>> _tagIndex = [];

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (memoryCache.TryGetValue(key, out CacheEntry<T>? cacheEntry) && cacheEntry != null)
            {
                metricsService.RecordHit("Memory");
                logger.LogInformation("Cache HIT for key: {Key}", key);
                return cacheEntry.Data;
            }

            metricsService.RecordMiss("Memory");
            logger.LogInformation("Cache MISS for key: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            metricsService.RecordError("Memory", "Get");
            logger.LogError(ex, "Error retrieving from cache with key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, CachePolicy policy)
    {
        try
        {
            var cacheEntry = new CacheEntry<T>
            {
                Data = value,
                CreatedAt = DateTime.UtcNow,
                Version = policy.Version,
                Metadata = []
            };

            var cacheOptions = new MemoryCacheEntryOptions
            {
                Priority = ConvertToCacheItemPriority(policy.Priority)
            };

            if (policy.AbsoluteExpiration.HasValue)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = policy.AbsoluteExpiration.Value;
                cacheEntry.ExpiresAt = DateTime.UtcNow.Add(policy.AbsoluteExpiration.Value);
            }

            if (policy.SlidingExpiration.HasValue)
            {
                cacheOptions.SlidingExpiration = policy.SlidingExpiration.Value;
            }

            // Add post-eviction callback for logging
            cacheOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (key, value, reason, state) =>
                {
                    logger.LogInformation("Cache entry '{Key}' was evicted due to: {Reason}", key, reason);
                }
            });

            // Store the enhanced cache entry
            memoryCache.Set(key, cacheEntry, cacheOptions);
            
            // Index by tags
            foreach (var tag in policy.Tags)
            {
                if (!_tagIndex.ContainsKey(tag))
                    _tagIndex[tag] = new List<string>();
                
                if (!_tagIndex[tag].Contains(key))
                    _tagIndex[tag].Add(key);
            }

            metricsService.RecordSet("Memory");
            logger.LogInformation("Set cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            metricsService.RecordError("Memory", "Set");
            logger.LogError(ex, "Error setting cache entry for key: {Key}", key);
        }
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            memoryCache.Remove(key);
            metricsService.RecordRemove("Memory");
            logger.LogInformation("Removed cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            metricsService.RecordError("Memory", "Remove");
            logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
        }
        
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(memoryCache.TryGetValue(key, out _));
    }

    public Task ClearAsync()
    {
        // Note: We cannot actually clear IMemoryCache without recreating it
        // This is a limitation of the IMemoryCache interface
        logger.LogWarning("ClearAsync called but cannot actually clear IMemoryCache without service recreation");
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetKeysByTagAsync(string tag)
    {
        var result = _tagIndex.ContainsKey(tag) ? _tagIndex[tag].AsEnumerable() : Enumerable.Empty<string>();
        return Task.FromResult(result);
    }

    public Task RemoveByTagAsync(string tag)
    {
        if (_tagIndex.ContainsKey(tag))
        {
            var keysToRemove = _tagIndex[tag].ToList();
            foreach (var key in keysToRemove)
            {
                memoryCache.Remove(key);
            }
            _tagIndex.Remove(tag);
        }
        
        return Task.CompletedTask;
    }

    private CacheItemPriority ConvertToCacheItemPriority(CachePriority priority)
    {
        return priority switch
        {
            CachePriority.Low => CacheItemPriority.Low,
            CachePriority.Normal => CacheItemPriority.Normal,
            CachePriority.High => CacheItemPriority.High,
            CachePriority.Critical => CacheItemPriority.NeverRemove,
            _ => CacheItemPriority.Normal
        };
    }
}

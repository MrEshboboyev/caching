using Caching.Api.Models;
using Caching.Api.Services.Interfaces;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Caching.Api.Services;

public class PartitionedCacheService : IEnhancedCacheService
{
    private readonly IList<IEnhancedCacheService> _partitions;
    private readonly ILogger<PartitionedCacheService> _logger;
    private readonly int _partitionCount;

    public PartitionedCacheService(
        ILogger<PartitionedCacheService> logger,
        int partitionCount = 4,
        params IEnhancedCacheService[] partitions)
    {
        _logger = logger;
        _partitionCount = partitionCount > 0 ? partitionCount : 4;
        
        // If partitions are provided, use them; otherwise create based on partition count
        if (partitions.Length > 0)
        {
            _partitions = [.. partitions];
        }
        else
        {
            // In a real implementation, these would be different cache instances
            // For this example, we'll use the same instance but in practice they would be different
            _partitions = [];
            for (int i = 0; i < _partitionCount; i++)
            {
                // This is a placeholder - in a real implementation, you would inject different cache instances
                _partitions.Add(partitions.Length > 0 ? partitions[0] : null!);
            }
        }
    }

    public PartitionedCacheService(
        ILogger<PartitionedCacheService> logger,
        params IEnhancedCacheService[] partitions) : this(logger, 4, partitions)
    {
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var partitionIndex = GetPartitionIndex(key);
            var partition = _partitions[partitionIndex];
            
            if (partition == null)
            {
                _logger.LogWarning("Partition {PartitionIndex} is not available", partitionIndex);
                return default;
            }

            _logger.LogInformation("Retrieving from partition {PartitionIndex} for key: {Key}", partitionIndex, key);
            return await partition.GetAsync<T>(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from partitioned cache with key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, CachePolicy policy)
    {
        try
        {
            var partitionIndex = GetPartitionIndex(key);
            var partition = _partitions[partitionIndex];
            
            if (partition == null)
            {
                _logger.LogWarning("Partition {PartitionIndex} is not available", partitionIndex);
                return;
            }

            _logger.LogInformation("Setting in partition {PartitionIndex} for key: {Key}", partitionIndex, key);
            await partition.SetAsync(key, value, policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting in partitioned cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var partitionIndex = GetPartitionIndex(key);
            var partition = _partitions[partitionIndex];
            
            if (partition == null)
            {
                _logger.LogWarning("Partition {PartitionIndex} is not available", partitionIndex);
                return;
            }

            _logger.LogInformation("Removing from partition {PartitionIndex} for key: {Key}", partitionIndex, key);
            await partition.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from partitioned cache for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var partitionIndex = GetPartitionIndex(key);
            var partition = _partitions[partitionIndex];
            
            if (partition == null)
            {
                _logger.LogWarning("Partition {PartitionIndex} is not available", partitionIndex);
                return false;
            }

            _logger.LogInformation("Checking existence in partition {PartitionIndex} for key: {Key}", partitionIndex, key);
            return await partition.ExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence in partitioned cache for key: {Key}", key);
            return false;
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            _logger.LogInformation("Clearing all partitions");
            foreach (var partition in _partitions)
            {
                if (partition != null)
                {
                    await partition.ClearAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing partitioned cache");
        }
    }

    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag)
    {
        try
        {
            _logger.LogInformation("Retrieving keys by tag {Tag} from all partitions", tag);
            var allKeys = new ConcurrentBag<string>();
            
            var tasks = _partitions
                .Where(p => p != null)
                .Select(async partition =>
                {
                    var keys = await partition.GetKeysByTagAsync(tag);
                    foreach (var key in keys)
                    {
                        allKeys.Add(key);
                    }
                });
            
            await Task.WhenAll(tasks);
            return allKeys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving keys by tag {Tag} from partitioned cache", tag);
            return Enumerable.Empty<string>();
        }
    }

    public async Task RemoveByTagAsync(string tag)
    {
        try
        {
            _logger.LogInformation("Removing entries by tag {Tag} from all partitions", tag);
            
            var tasks = _partitions
                .Where(p => p != null)
                .Select(partition => partition.RemoveByTagAsync(tag));
            
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing entries by tag {Tag} from partitioned cache", tag);
        }
    }

    private int GetPartitionIndex(string key)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var hashInt = BitConverter.ToUInt32(hashBytes, 0);
        return (int)(hashInt % (uint)_partitions.Count);
    }
}

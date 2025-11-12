using Caching.Api.Models;
using Caching.Api.Services.Interfaces;
using MessagePack;
using Microsoft.Extensions.Caching.Distributed;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caching.Api.Services;

public class EnhancedDistributedCacheService : IEnhancedCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ICacheMetricsService _metricsService;
    private readonly ILogger<EnhancedDistributedCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EnhancedDistributedCacheService(
        IDistributedCache distributedCache,
        ICacheMetricsService metricsService,
        ILogger<EnhancedDistributedCacheService> logger)
    {
        _distributedCache = distributedCache;
        _metricsService = metricsService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var data = await _distributedCache.GetAsync(key);
            if (data != null && data.Length > 0)
            {
                // Check serialization format
                var format = GetSerializationFormat(data);
                
                byte[] payload;
                if (IsCompressed(data))
                {
                    payload = await DecompressDataAsync(data[1..]); // Skip the compression flag
                    _logger.LogInformation("Data decompressed for key: {Key}", key);
                }
                else
                {
                    payload = data[1..]; // Skip the format flag
                }

                CacheEntry<T>? cacheEntry = null;
                if (format == SerializationFormat.Json)
                {
                    var jsonData = Encoding.UTF8.GetString(payload);
                    cacheEntry = JsonSerializer.Deserialize<CacheEntry<T>>(jsonData, _jsonOptions);
                }
                else if (format == SerializationFormat.MessagePack)
                {
                    cacheEntry = MessagePackSerializer.Deserialize<CacheEntry<T>>(payload);
                }

                _metricsService.RecordHit("Distributed");
                _logger.LogInformation("Cache HIT for key: {Key} with format: {Format}", key, format);
                if (cacheEntry != null)
                {
                    return cacheEntry.Data;
                }
            }

            _metricsService.RecordMiss("Distributed");
            _logger.LogInformation("Cache MISS for key: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _metricsService.RecordError("Distributed", "Get");
            _logger.LogError(ex, "Error retrieving from distributed cache with key: {Key}", key);
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

            var options = new DistributedCacheEntryOptions();

            if (policy.AbsoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = policy.AbsoluteExpiration.Value;
                cacheEntry.ExpiresAt = DateTime.UtcNow.Add(policy.AbsoluteExpiration.Value);
            }

            if (policy.SlidingExpiration.HasValue)
            {
                options.SlidingExpiration = policy.SlidingExpiration.Value;
            }

            byte[] payload;
            if (policy.SerializationFormat == SerializationFormat.Json)
            {
                var jsonData = JsonSerializer.Serialize(cacheEntry, _jsonOptions);
                payload = Encoding.UTF8.GetBytes(jsonData);
            }
            else
            {
                payload = MessagePackSerializer.Serialize(cacheEntry);
            }

            // Apply compression if requested
            if (policy.UseCompression)
            {
                payload = await CompressDataAsync(payload);
                _logger.LogInformation("Data compressed for key: {Key}", key);
            }

            // Prepend format flag
            var data = new byte[payload.Length + 1];
            data[0] = (byte)(policy.UseCompression ? (0x80 | (byte)policy.SerializationFormat) : (byte)policy.SerializationFormat);
            Array.Copy(payload, 0, data, 1, payload.Length);

            await _distributedCache.SetAsync(key, data, options);

            _metricsService.RecordSet("Distributed");
            _logger.LogInformation("Set distributed cache entry for key: {Key} with format: {Format}", key, policy.SerializationFormat);
        }
        catch (Exception ex)
        {
            _metricsService.RecordError("Distributed", "Set");
            _logger.LogError(ex, "Error setting distributed cache entry for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
            _metricsService.RecordRemove("Distributed");
            _logger.LogInformation("Removed distributed cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _metricsService.RecordError("Distributed", "Remove");
            _logger.LogError(ex, "Error removing distributed cache entry for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var data = await _distributedCache.GetAsync(key);
            return data != null && data.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of distributed cache entry for key: {Key}", key);
            return false;
        }
    }

    public Task ClearAsync()
    {
        // Note: IDistributedCache doesn't have a clear method
        // In a real implementation, you would need to track keys separately
        _logger.LogWarning("ClearAsync called but IDistributedCache doesn't support clearing all entries");
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetKeysByTagAsync(string tag)
    {
        // IDistributedCache doesn't provide a way to enumerate keys
        // In a real implementation, you would need to maintain a separate index
        _logger.LogWarning("GetKeysByTagAsync called but IDistributedCache doesn't support key enumeration");
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task RemoveByTagAsync(string tag)
    {
        // IDistributedCache doesn't provide a way to enumerate keys
        // In a real implementation, you would need to maintain a separate index
        _logger.LogWarning("RemoveByTagAsync called but IDistributedCache doesn't support key enumeration");
        return Task.CompletedTask;
    }

    private SerializationFormat GetSerializationFormat(byte[] data)
    {
        if (data.Length == 0) return SerializationFormat.Json;
        var formatByte = data[0];
        // Clear compression bit if set
        var format = formatByte & 0x7F;
        return (SerializationFormat)format;
    }

    private bool IsCompressed(byte[] data)
    {
        if (data.Length == 0) return false;
        // Check if compression bit is set
        return (data[0] & 0x80) != 0;
    }

    private async Task<byte[]> CompressDataAsync(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
        {
            await gzipStream.WriteAsync(data);
        }
        return outputStream.ToArray();
    }

    private async Task<byte[]> DecompressDataAsync(byte[] compressedData)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        await gzipStream.CopyToAsync(outputStream);
        return outputStream.ToArray();
    }
}

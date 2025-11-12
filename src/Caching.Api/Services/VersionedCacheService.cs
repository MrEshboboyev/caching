using Caching.Api.Models;
using Caching.Api.Services.Interfaces;

namespace Caching.Api.Services;

public class VersionedCacheService(
    IEnhancedCacheService cacheService,
    ILogger<VersionedCacheService> logger,
    string currentSchemaVersion = "1.0"
) : IEnhancedCacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cacheEntry = await cacheService.GetAsync<CacheEntry<T>>(key);
            
            if (cacheEntry == null)
            {
                logger.LogInformation("No cache entry found for key: {Key}", key);
                return default;
            }

            // Check version compatibility
            if (!IsVersionCompatible(cacheEntry.SchemaVersion, cacheEntry.CompatibilityMode))
            {
                logger.LogWarning("Cache entry for key {Key} has incompatible version {Version}, current version is {CurrentVersion}", 
                    key, cacheEntry.SchemaVersion, currentSchemaVersion);
                
                // Attempt to migrate if possible
                var migratedEntry = await MigrateCacheEntryAsync(cacheEntry, key);
                if (migratedEntry != null)
                {
                    logger.LogInformation("Successfully migrated cache entry for key: {Key}", key);
                    return migratedEntry.Data;
                }
                
                return default;
            }

            logger.LogInformation("Successfully retrieved cache entry for key: {Key} with version: {Version}", 
                key, cacheEntry.SchemaVersion);
            return cacheEntry.Data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving versioned cache entry for key: {Key}", key);
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
                SchemaVersion = currentSchemaVersion,
                Metadata = [],
                CompatibilityMode = CompatibilityMode.Compatible
            };

            // Add version metadata
            cacheEntry.Metadata["schemaVersion"] = currentSchemaVersion;
            cacheEntry.Metadata["compatibilityMode"] = policy.SerializationFormat.ToString();
            
            await cacheService.SetAsync(key, cacheEntry, policy);
            logger.LogInformation("Successfully stored versioned cache entry for key: {Key} with version: {Version}", 
                key, policy.Version);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting versioned cache entry for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await cacheService.RemoveAsync(key);
            logger.LogInformation("Successfully removed versioned cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing versioned cache entry for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await cacheService.ExistsAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking existence of versioned cache entry for key: {Key}", key);
            return false;
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await cacheService.ClearAsync();
            logger.LogInformation("Successfully cleared versioned cache");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing versioned cache");
        }
    }

    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag)
    {
        try
        {
            return await cacheService.GetKeysByTagAsync(tag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving versioned cache keys by tag: {Tag}", tag);
            return [];
        }
    }

    public async Task RemoveByTagAsync(string tag)
    {
        try
        {
            await cacheService.RemoveByTagAsync(tag);
            logger.LogInformation("Successfully removed versioned cache entries by tag: {Tag}", tag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing versioned cache entries by tag: {Tag}", tag);
        }
    }

    private bool IsVersionCompatible(string entryVersion, CompatibilityMode mode)
    {
        if (mode == CompatibilityMode.Lenient)
            return true;

        if (mode == CompatibilityMode.Strict)
            return entryVersion == currentSchemaVersion;

        // For Compatible mode, check if major versions match
        var entryParts = entryVersion.Split('.');
        var currentParts = currentSchemaVersion.Split('.');
        
        if (entryParts.Length >= 1 && currentParts.Length >= 1)
        {
            return entryParts[0] == currentParts[0]; // Major version match
        }
        
        return entryVersion == currentSchemaVersion;
    }

    private async Task<CacheEntry<T>?> MigrateCacheEntryAsync<T>(CacheEntry<T> entry, string key)
    {
        try
        {
            logger.LogInformation("Attempting to migrate cache entry for key: {Key} from version {FromVersion} to {ToVersion}", 
                key, entry.SchemaVersion, currentSchemaVersion);

            // Simple version migration - in a real implementation, this would be more complex
            if (CanMigrate(entry.SchemaVersion, currentSchemaVersion))
            {
                var migratedEntry = new CacheEntry<T>
                {
                    Data = entry.Data,
                    CreatedAt = entry.CreatedAt,
                    ExpiresAt = entry.ExpiresAt,
                    Version = entry.Version,
                    SchemaVersion = currentSchemaVersion,
                    Metadata = entry.Metadata,
                    CompatibilityMode = CompatibilityMode.Compatible
                };

                // Update the cache with the migrated entry
                // Note: We would need the policy to do this properly
                logger.LogInformation("Migration successful for key: {Key}", key);
                return migratedEntry;
            }

            logger.LogWarning("Cannot migrate cache entry for key: {Key} from version {FromVersion} to {ToVersion}", 
                key, entry.SchemaVersion, currentSchemaVersion);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error migrating cache entry for key: {Key}", key);
            return null;
        }
    }

    private bool CanMigrate(string fromVersion, string toVersion)
    {
        // Simple migration logic - in a real implementation, this would be more sophisticated
        var fromParts = fromVersion.Split('.');
        var toParts = toVersion.Split('.');
        
        // Allow migration if:
        // 1. Same major version, or
        // 2. From version is older than to version
        if (fromParts.Length >= 1 && toParts.Length >= 1)
        {
            if (fromParts[0] == toParts[0]) // Same major version
                return true;
                
            if (int.TryParse(fromParts[0], out var fromMajor) && 
                int.TryParse(toParts[0], out var toMajor) && 
                fromMajor < toMajor) // From is older
                return true;
        }
        
        return false;
    }
}

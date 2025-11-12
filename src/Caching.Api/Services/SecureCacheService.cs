using Caching.Api.Models;
using Caching.Api.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Caching.Api.Services;

public class SecureCacheService : IEnhancedCacheService
{
    private readonly IEnhancedCacheService _cacheService;
    private readonly ILogger<SecureCacheService> _logger;
    private readonly byte[] _encryptionKey;
    private readonly byte[] _iv;

    public SecureCacheService(
        IEnhancedCacheService cacheService,
        ILogger<SecureCacheService> logger,
        string encryptionKey = "DefaultSecureKey12345") // In production, this should come from configuration
    {
        _cacheService = cacheService;
        _logger = logger;

        // Generate a proper encryption key and IV
        _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
        _iv = new byte[16]; // AES block size
        Array.Copy(_encryptionKey, 0, _iv, 0, 16);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var encryptedKey = EncryptString(key);
            var encryptedData = await _cacheService.GetAsync<string>(encryptedKey);
            
            if (string.IsNullOrEmpty(encryptedData))
            {
                _logger.LogInformation("No encrypted data found for key: {Key}", key);
                return default;
            }

            var decryptedData = DecryptString(encryptedData);
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(decryptedData);
            
            _logger.LogInformation("Successfully decrypted data for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secure cache entry for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, CachePolicy policy)
    {
        try
        {
            var encryptedKey = EncryptString(key);
            var jsonData = System.Text.Json.JsonSerializer.Serialize(value);
            var encryptedData = EncryptString(jsonData);
            
            // Add security metadata
            policy.Tags.Add("secure");
            policy.Tags.Add($"secured-{DateTime.UtcNow:yyyyMMdd}");
            
            await _cacheService.SetAsync(encryptedKey, encryptedData, policy);
            _logger.LogInformation("Successfully encrypted and stored data for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting secure cache entry for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var encryptedKey = EncryptString(key);
            await _cacheService.RemoveAsync(encryptedKey);
            _logger.LogInformation("Successfully removed secure cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing secure cache entry for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var encryptedKey = EncryptString(key);
            return await _cacheService.ExistsAsync(encryptedKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of secure cache entry for key: {Key}", key);
            return false;
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _cacheService.ClearAsync();
            _logger.LogInformation("Successfully cleared secure cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing secure cache");
        }
    }

    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag)
    {
        try
        {
            // Note: In a secure implementation, we wouldn't expose actual keys
            // This is a simplified implementation for demonstration
            return await _cacheService.GetKeysByTagAsync(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secure cache keys by tag: {Tag}", tag);
            return [];
        }
    }

    public async Task RemoveByTagAsync(string tag)
    {
        try
        {
            await _cacheService.RemoveByTagAsync(tag);
            _logger.LogInformation("Successfully removed secure cache entries by tag: {Tag}", tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing secure cache entries by tag: {Tag}", tag);
        }
    }

    private string EncryptString(string plainText)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = _iv;
            
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }
            
            var encrypted = msEncrypt.ToArray();
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting string");
            return plainText; // Fallback to plain text in case of encryption failure
        }
    }

    private string DecryptString(string cipherText)
    {
        try
        {
            var encrypted = Convert.FromBase64String(cipherText);
            
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = _iv;
            
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(encrypted);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting string");
            return cipherText; // Fallback to cipher text in case of decryption failure
        }
    }
}

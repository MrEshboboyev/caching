using Caching.Api.Models;
using Caching.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Caching.Api.Services;

public class ProductCachingService(IMemoryCache memoryCache, IDistributedCache redisCache, IProductService productService) : IProductCachingService
{
    private const string CacheKey = "products-cache";

    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        return await GetFromMemoryCache() ?? await GetFromRedisCache() ?? await GetFromDatabase();
    }

    public async Task<IEnumerable<Product>> GetProductsWithSlidingExpirationAsync()
    {
        return await GetFromMemoryCache() ?? await GetFromRedisCacheWithSlidingExpiration() ?? await GetFromDatabaseWithSlidingExpiration();
    }

    public async Task<IEnumerable<Product>> GetProductsWithPostEvictionCallbackAsync()
    {
        return await GetFromMemoryCache() ?? await GetFromRedisCacheWithPostEvictionCallback() ?? await GetFromDatabaseWithPostEvictionCallback();
    }

    public async Task<IEnumerable<Product>> GetProductsWithDependencyAsync()
    {
        return await GetFromMemoryCache() ?? await GetFromRedisCacheWithDependency() ?? await GetFromDatabaseWithDependency();
    }

    private Task<IEnumerable<Product>?> GetFromMemoryCache()
    {
        if (memoryCache.TryGetValue(CacheKey, out IEnumerable<Product>? products))
        {
            Console.WriteLine("Data from Memory Cache");
            return Task.FromResult(products);
        }
        return Task.FromResult<IEnumerable<Product>?>(null);
    }

    private async Task<IEnumerable<Product>?> GetFromRedisCache()
    {
        var redisData = await redisCache.GetStringAsync(CacheKey);
        if (redisData is not null)
        {
            Console.WriteLine("Data from Redis");
            var products = JsonSerializer.Deserialize<IEnumerable<Product>>(redisData);
            memoryCache.Set(CacheKey, products, TimeSpan.FromSeconds(10));
            return products;
        }
        return null;
    }

    private async Task<IEnumerable<Product>> GetFromDatabase()
    {
        var products = productService.GetAll();
        Console.WriteLine("Data from Database");

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };

        await redisCache.SetStringAsync(CacheKey, JsonSerializer.Serialize(products), options);
        memoryCache.Set(CacheKey, products, TimeSpan.FromSeconds(10));

        return products;
    }

    private async Task<IEnumerable<Product>?> GetFromRedisCacheWithSlidingExpiration()
    {
        var redisData = await redisCache.GetStringAsync(CacheKey);
        if (redisData is not null)
        {
            Console.WriteLine("Data from Redis");
            var products = JsonSerializer.Deserialize<IEnumerable<Product>>(redisData);
            memoryCache.Set(CacheKey, products, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(10)
            });
            return products;
        }
        return null;
    }

    private async Task<IEnumerable<Product>> GetFromDatabaseWithSlidingExpiration()
    {
        var products = productService.GetAll();
        Console.WriteLine("Data from Database");

        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(10)
        };

        await redisCache.SetStringAsync(CacheKey, JsonSerializer.Serialize(products), options);
        memoryCache.Set(CacheKey, products, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(10)
        });

        return products;
    }

    private async Task<IEnumerable<Product>?> GetFromRedisCacheWithPostEvictionCallback()
    {
        var redisData = await redisCache.GetStringAsync(CacheKey);
        if (redisData is not null)
        {
            Console.WriteLine("Data from Redis");
            var products = JsonSerializer.Deserialize<IEnumerable<Product>>(redisData);
            memoryCache.Set(CacheKey, products, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
                PostEvictionCallbacks =
                {
                    new PostEvictionCallbackRegistration
                    {
                        EvictionCallback = (key, value, reason, state) =>
                        {
                            Console.WriteLine($"Cache entry '{key}' was removed due to '{reason}'.");
                        }
                    }
                }
            });
            return products;
        }
        return null;
    }

    private async Task<IEnumerable<Product>> GetFromDatabaseWithPostEvictionCallback()
    {
        var products = productService.GetAll();
        Console.WriteLine("Data from Database");

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };

        await redisCache.SetStringAsync(CacheKey, JsonSerializer.Serialize(products), options);
        memoryCache.Set(CacheKey, products, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (key, value, reason, state) =>
                    {
                        Console.WriteLine($"Cache entry '{key}' was removed due to '{reason}'.");
                    }
                }
            }
        });

        return products;
    }

    private async Task<IEnumerable<Product>?> GetFromRedisCacheWithDependency()
    {
        var redisData = await redisCache.GetStringAsync(CacheKey);
        if (redisData is not null)
        {
            Console.WriteLine("Data from Redis");
            var products = JsonSerializer.Deserialize<IEnumerable<Product>>(redisData);
            memoryCache.Set(CacheKey, products, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            });
            return products;
        }
        return null;
    }

    private async Task<IEnumerable<Product>> GetFromDatabaseWithDependency()
    {
        var products = productService.GetAll();
        Console.WriteLine("Data from Database");

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };

        await redisCache.SetStringAsync(CacheKey, JsonSerializer.Serialize(products), options);
        memoryCache.Set(CacheKey, products, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
        });

        return products;
    }
}

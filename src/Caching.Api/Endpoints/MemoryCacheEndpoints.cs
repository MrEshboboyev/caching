using Caching.Api.Models;
using Caching.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Caching.Api.Endpoints;

public static class MemoryCacheEndpoints
{
    public static void MapMemoryCacheEndpoints(this IEndpointRouteBuilder app)
    {
        // Endpoint with absolute expiration
        app.MapGet("/absolute-cache-products",
            ([FromServices] IMemoryCache cache,
            [FromServices] IProductService productService) =>
            {
                var cacheKey = "products-cache";

                // Try to get the cached products
                if (!cache.TryGetValue(cacheKey, out IEnumerable<Product>? products))
                {
                    // If not found in cache, get products from the service
                    products = productService.GetAll();
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        // Set cache to expire after 10 seconds
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                    };
                    // Set the products in cache
                    cache.Set(cacheKey, products, cacheOptions);
                }

                // Return the products
                return Results.Ok(products);
            });

        // Endpoint with sliding expiration
        app.MapGet("/sliding-cache-products",
            ([FromServices] IMemoryCache cache,
            [FromServices] IProductService productService) =>
            {
                var cacheKey = "sliding-products-cache";

                // Try to get the cached products
                if (!cache.TryGetValue(cacheKey, out IEnumerable<Product>? products))
                {
                    // If not found in cache, get products from the service
                    products = productService.GetAll();
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        // Set cache to expire if not accessed for 10 seconds
                        SlidingExpiration = TimeSpan.FromSeconds(10)
                    };
                    // Set the products in cache
                    cache.Set(cacheKey, products, cacheOptions);
                }

                // Return the products
                return Results.Ok(products);
            });

        // Endpoint with post-eviction callback
        app.MapGet("/eviction-callback-cache-products",
            ([FromServices] IMemoryCache cache,
            [FromServices] IProductService productService) =>
            {
                var cacheKey = "eviction-callback-products-cache";

                // Try to get the cached products
                if (!cache.TryGetValue(cacheKey, out IEnumerable<Product>? products))
                {
                    // If not found in cache, get products from the service
                    products = productService.GetAll();
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        // Set cache to expire after 10 seconds
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
                        // Add a post-eviction callback
                        PostEvictionCallbacks =
                        {
                            new PostEvictionCallbackRegistration
                            {
                                EvictionCallback = (key, value, reason, state) =>
                                {
                                    // Log the reason for eviction
                                    Console.WriteLine($"Cache entry '{key}' was removed due to '{reason}'.");
                                }
                            }
                        }
                    };
                    // Set the products in cache
                    cache.Set(cacheKey, products, cacheOptions);
                }

                // Return the products
                return Results.Ok(products);
            });

        // Endpoint with cache priority
        app.MapGet("/priority-cache-products",
            ([FromServices] IMemoryCache cache,
            [FromServices] IProductService productService) =>
            {
                var cacheKey = "priority-products-cache";

                // Try to get the cached products
                if (!cache.TryGetValue(cacheKey, out IEnumerable<Product>? products))
                {
                    // If not found in cache, get products from the service
                    products = productService.GetAll();
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        // Set cache to expire after 10 seconds
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
                        // Set cache priority to high
                        Priority = CacheItemPriority.High
                    };
                    // Set the products in cache
                    cache.Set(cacheKey, products, cacheOptions);
                }

                // Return the products
                return Results.Ok(products);
            });

        // Endpoint with cache dependency
        app.MapGet("/dependent-cache-products",
            ([FromServices] IMemoryCache cache,
            [FromServices] IProductService productService) =>
            {
                var cacheKey = "dependent-products-cache";
                var dependencyKey = "dependency-key";

                // Try to get the cached products
                if (!cache.TryGetValue(cacheKey, out IEnumerable<Product>? products))
                {
                    // If not found in cache, get products from the service
                    products = productService.GetAll();
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        // Set cache to expire after 10 seconds
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                    };
                    // Set the products in cache
                    cache.Set(cacheKey, products, cacheOptions);

                    // Set a dependent cache entry
                    cache.Set(dependencyKey, "dependency-value", new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
                    });

                    // Link the main cache entry to the dependent entry
                    cacheOptions.AddExpirationToken(new CancellationChangeToken(
                        new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token));
                }

                // Return the products
                return Results.Ok(products);
            });
    }
}

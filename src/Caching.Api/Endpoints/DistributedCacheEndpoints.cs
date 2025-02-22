using Caching.Api.Models;
using Caching.Api.Services;
using Caching.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Caching.Api.Endpoints;

public static class DistributedCacheEndpoints
{
    public static void MapDistributedCacheEndpoints(this IEndpointRouteBuilder app)
    {
        // Memory + Redis Cache
        app.MapGet("/memory-redis-cache-products",
            async ([FromServices] IProductCachingService productCachingService) =>
            {
                var products = await productCachingService.GetProductsAsync();
                return Results.Ok(products);
            });

        // Memory + Redis Cache with Sliding Expiration
        app.MapGet("/memory-redis-cache-products-sliding",
            async ([FromServices] IProductCachingService productCachingService) =>
            {
                var products = await productCachingService.GetProductsWithSlidingExpirationAsync();
                return Results.Ok(products);
            });

        // Memory + Redis Cache with Post Eviction Callback
        app.MapGet("/memory-redis-cache-products-eviction-callback",
            async ([FromServices] IProductCachingService productCachingService) =>
            {
                var products = await productCachingService.GetProductsWithPostEvictionCallbackAsync();
                return Results.Ok(products);
            });

        // Memory + Redis Cache with Dependency
        app.MapGet("/memory-redis-cache-products-dependency",
            async ([FromServices] IProductCachingService productCachingService) =>
            {
                var products = await productCachingService.GetProductsWithDependencyAsync();
                return Results.Ok(products);
            });
    }
}

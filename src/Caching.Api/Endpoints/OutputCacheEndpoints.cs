using Caching.Api.Services.Interfaces;

namespace Caching.Api.Endpoints;

public static class OutputCacheEndpoints
{
    public static void MapOutputCacheEndpoints(this IEndpointRouteBuilder app)
    {
        // Output caching
        app.MapGet("/output-cache-products", (IProductService productService) =>
        {
            return Results.Ok(productService.GetAll());
        })
        .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30))); // Enable output caching with a 30-second expiration

        // Output caching
        app.MapGet("/output-cache", () =>
        {
            return Results.Ok(
                new
                {
                    Message = "This is a output cache",
                    Timestamp = DateTime.UtcNow.Second
                }
            );
        })
        .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30))); // Enable output caching with a 30-second expiration
    }
}

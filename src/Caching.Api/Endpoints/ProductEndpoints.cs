using Caching.Api.Services.Interfaces;

namespace Caching.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        // without caching
        app.MapGet("/products", (IProductService productService) =>
        {
            return Results.Ok(productService.GetAll());
        })
        .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30))); // Enable output caching with a 30-second expiration

        // Output caching
        app.MapGet("/output-cache-products", (IProductService productService) =>
        {
            return Results.Ok(productService.GetAll());
        })
        .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30))); // Enable output caching with a 30-second expiration


        // Response caching
        app.MapGet("/response-cache-products", 
            (IProductService productService,
            HttpContext context) =>
        {
            context.Response.GetTypedHeaders().CacheControl = new()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(30)
            };

            return Results.Ok(productService.GetAll());
        });
    }
}
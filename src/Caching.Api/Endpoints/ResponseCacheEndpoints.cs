using Caching.Api.Services.Interfaces;
using Microsoft.Net.Http.Headers;

namespace Caching.Api.Endpoints;

public static class ResponseCacheEndpoints
{
    public static void MapResponseCacheEndpoints(this IEndpointRouteBuilder app)
    {
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

        // Response caching
        app.MapGet("/response-cache",
            (HttpContext context) =>
            {
                context.Response.GetTypedHeaders().CacheControl = new()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(30)
                };

                context.Response.Headers[HeaderNames.Vary] = "Accept-Encoding";

                return Results.Ok(
                    new
                    {
                        Message = "This is a response cache",
                        Timestamp = DateTime.UtcNow.Second
                    }
                );
            });
    }
}
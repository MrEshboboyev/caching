﻿using Caching.Api.Services.Interfaces;

namespace Caching.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        // without caching
        app.MapGet("/products", (IProductService productService) =>
        {
            return Results.Ok(productService.GetAll());
        });
    }
}
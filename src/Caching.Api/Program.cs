using Caching.Api.Endpoints;
using Caching.Api.Models;
using Caching.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

#region Endpoints

app.MapProductEndpoints(); // Use the extension method to map product endpoints
app.MapMemoryCacheEndpoints(); // Use the extension method to map memory cache endpoints

#endregion

app.Run();
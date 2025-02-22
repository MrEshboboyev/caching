using Caching.Api.Endpoints;
using Caching.Api.Services;
using Caching.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString");

});
// Add Output Caching services
builder.Services.AddOutputCache();
builder.Services.AddResponseCaching();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductCachingService, ProductCachingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    Console.WriteLine($"[{DateTime.UtcNow}] Processing request: {context.Request.Method} {context.Request.Path}");
    await next();
});

app.UseOutputCache();
app.UseResponseCaching();

app.UseHttpsRedirection();

#region Endpoints

app.MapProductEndpoints(); // Use the extension method to map product endpoints
app.MapMemoryCacheEndpoints(); // Use the extension method to map memory cache endpoints
app.MapDistributedCacheEndpoints(); // Use the extension method to map distributed cache endpoints
app.MapOutputCacheEndpoints(); // Use the extension method to map output cache endpoints
app.MapResponseCacheEndpoints(); // Use the extension method to map response cache endpoints

#endregion

app.Run();
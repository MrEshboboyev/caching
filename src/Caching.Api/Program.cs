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
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(30)));
});

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductCachingService, ProductCachingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

#region Endpoints

app.MapProductEndpoints(); // Use the extension method to map product endpoints
app.MapMemoryCacheEndpoints(); // Use the extension method to map memory cache endpoints
app.MapDistributedCacheEndpoints(); // Use the extension method to map distributed cache endpoints

#endregion

app.Run();
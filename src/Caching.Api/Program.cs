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

// Register our enhanced caching services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICacheMetricsService, CacheMetricsService>();
builder.Services.AddScoped<IEnhancedCacheService, EnhancedMemoryCacheService>();
builder.Services.AddScoped<EnhancedDistributedCacheService>();

// Register composite cache service that combines memory and distributed cache
builder.Services.AddScoped<IEnhancedCacheService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<CompositeCacheService>>();
    var memoryCache = serviceProvider.GetRequiredService<EnhancedMemoryCacheService>();
    var distributedCache = serviceProvider.GetRequiredService<EnhancedDistributedCacheService>();
    return new CompositeCacheService(logger, memoryCache, distributedCache);
});

// Register partitioned cache service
builder.Services.AddScoped<IEnhancedCacheService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<PartitionedCacheService>>();
    var compositeCache = serviceProvider.GetRequiredService<IEnhancedCacheService>();
    return new PartitionedCacheService(logger, compositeCache);
});

// Register secure cache service
builder.Services.AddScoped<IEnhancedCacheService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<SecureCacheService>>();
    var partitionedCache = serviceProvider.GetRequiredService<IEnhancedCacheService>();
    var encryptionKey = builder.Configuration.GetValue<string>("CacheEncryptionKey") ?? "DefaultSecureKey12345";
    return new SecureCacheService(partitionedCache, logger, encryptionKey);
});

// Register versioned cache service
builder.Services.AddScoped<IEnhancedCacheService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<VersionedCacheService>>();
    var secureCache = serviceProvider.GetRequiredService<IEnhancedCacheService>();
    var currentSchemaVersion = builder.Configuration.GetValue<string>("CacheSchemaVersion") ?? "1.0";
    return new VersionedCacheService(secureCache, logger, currentSchemaVersion);
});

// Register diagnostics service
builder.Services.AddScoped<DiagnosticsService>();

// Register resilient cache service as a decorator
builder.Services.Decorate<IEnhancedCacheService>((inner, serviceProvider) =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<ResilientCacheService>>();
    return new ResilientCacheService(inner, logger);
});

// Register cache warming service as a hosted service
builder.Services.AddHostedService<CacheWarmingService>();
builder.Services.AddScoped<CacheWarmingService>();

// Add controllers
builder.Services.AddControllers();

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

// Add controllers
app.MapControllers();

#region Endpoints

app.MapProductEndpoints(); // Use the extension method to map product endpoints
app.MapMemoryCacheEndpoints(); // Use the extension method to map memory cache endpoints
app.MapDistributedCacheEndpoints(); // Use the extension method to map distributed cache endpoints
app.MapOutputCacheEndpoints(); // Use the extension method to map output cache endpoints
app.MapResponseCacheEndpoints(); // Use the extension method to map response cache endpoints

#endregion

app.Run();

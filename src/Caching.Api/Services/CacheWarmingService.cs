using Caching.Api.Models;
using Caching.Api.Services.Interfaces;

namespace Caching.Api.Services;

public class CacheWarmingService(
    IEnhancedCacheService cacheService,
    IProductService productService,
    ILogger<CacheWarmingService> logger
) : IHostedService
{
    private Timer? _timer;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting cache warming service");

        // Perform initial cache warming
        await WarmupProductCacheAsync();

        // Schedule periodic cache warming
        _timer = new Timer(async (state) => await WarmupProductCacheAsync(), null, 
            TimeSpan.FromMinutes(5), // Start after 5 minutes
            TimeSpan.FromMinutes(10)); // Repeat every 10 minutes

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping cache warming service");
        _timer?.Change(Timeout.Infinite, 0);
        await Task.CompletedTask;
    }

    public async Task WarmupProductCacheAsync()
    {
        try
        {
            logger.LogInformation("Starting product cache warmup");

            // Get products from the data source
            var products = productService.GetAll();

            // Create cache policy with high priority and longer expiration
            var policy = new CachePolicy
            {
                AbsoluteExpiration = TimeSpan.FromMinutes(15),
                Priority = CachePriority.Critical,
                Tags = new List<string> { "products", "warmup", "critical" }
            };

            // Cache the products
            await cacheService.SetAsync("products-all", products, policy);

            logger.LogInformation("Product cache warmup completed successfully with {Count} products", products.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during product cache warmup");
        }
    }

    public async Task WarmupCacheWithRetryAsync(int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await WarmupProductCacheAsync();
                return; // Success
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cache warmup attempt {Attempt} failed", i + 1);
                if (i == maxRetries - 1) throw; // Re-throw on final attempt

                // Wait before retry with exponential backoff
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
        }
    }

    public async Task WarmupProductCacheByCategoryAsync(string category)
    {
        try
        {
            logger.LogInformation("Starting product cache warmup for category: {Category}", category);

            // Get products from the data source (in a real app, this would filter by category)
            var products = productService.GetAll().Where(p => p.Name.Contains(category, StringComparison.OrdinalIgnoreCase));

            // Create cache policy
            var policy = new CachePolicy
            {
                AbsoluteExpiration = TimeSpan.FromMinutes(10),
                Priority = CachePriority.High,
                Tags = ["products", "category", category]
            };

            // Cache the products
            await cacheService.SetAsync($"products-category-{category}", products, policy);

            logger.LogInformation("Product cache warmup for category {Category} completed successfully with {Count} products", 
                category, products.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during product cache warmup for category: {Category}", category);
        }
    }
}

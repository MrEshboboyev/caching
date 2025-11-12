using Caching.Api.Models;
using Caching.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Caching.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController(
    IEnhancedCacheService cacheService,
    ICacheMetricsService metricsService
) : ControllerBase
{
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var products = await cacheService.GetAsync<IEnumerable<Product>>("products-all");
        if (products == null)
        {
            return NotFound("Products not found in cache");
        }

        return Ok(products);
    }

    [HttpPost("products")]
    public async Task<IActionResult> SetProducts([FromBody] IEnumerable<Product> products, [FromQuery] int expirationMinutes = 10)
    {
        var policy = new CachePolicy
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(expirationMinutes),
            Priority = CachePriority.High,
            Tags = ["products", "user-generated"]
        };

        await cacheService.SetAsync("products-all", products, policy);
        return Ok("Products cached successfully");
    }

    [HttpDelete("products")]
    public async Task<IActionResult> RemoveProducts()
    {
        await cacheService.RemoveAsync("products-all");
        return Ok("Products cache removed");
    }

    [HttpGet("metrics/{cacheType}")]
    public IActionResult GetMetrics(string cacheType)
    {
        var metrics = metricsService.GetMetrics(cacheType);
        return Ok(metrics);
    }

    [HttpPost("warmup")]
    public IActionResult WarmupCache()
    {
        // In a real implementation, this would trigger cache warming
        return Ok("Cache warming initiated");
    }

    [HttpGet("exists/{key}")]
    public async Task<IActionResult> CheckExists(string key)
    {
        var exists = await cacheService.ExistsAsync(key);
        return Ok(new { Key = key, Exists = exists });
    }

    [HttpDelete("tags/{tag}")]
    public async Task<IActionResult> RemoveByTag(string tag)
    {
        await cacheService.RemoveByTagAsync(tag);
        return Ok($"Entries with tag '{tag}' removed");
    }
}

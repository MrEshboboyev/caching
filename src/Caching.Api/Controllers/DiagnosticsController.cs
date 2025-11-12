using Caching.Api.Services;
using Caching.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Caching.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController(
    DiagnosticsService diagnosticsService,
    IEnhancedCacheService cacheService,
    ICacheMetricsService metricsService,
    ILogger<DiagnosticsController> logger
) : ControllerBase
{
    [HttpGet("diagnostics")]
    public IActionResult GetDiagnostics()
    {
        try
        {
            var diagnostics = diagnosticsService.GetDiagnostics();
            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving diagnostics");
            return StatusCode(500, "Error retrieving diagnostics");
        }
    }

    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        try
        {
            var metrics = metricsService.GetAllMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    [HttpPost("health")]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            var healthCheck = await diagnosticsService.PerformHealthCheckAsync(cacheService);
            return Ok(healthCheck);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing health check");
            return StatusCode(500, "Error performing health check");
        }
    }

    [HttpPost("reset-metrics")]
    public IActionResult ResetMetrics()
    {
        try
        {
            metricsService.ResetAllMetrics();
            return Ok("Metrics reset successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting metrics");
            return StatusCode(500, "Error resetting metrics");
        }
    }

    [HttpGet("cache-stats")]
    public IActionResult GetCacheStats()
    {
        try
        {
            var metrics = metricsService.GetAllMetrics();
            var stats = new CacheStats
            {
                TotalHits = metrics.Values.Sum(m => m.Hits),
                TotalMisses = metrics.Values.Sum(m => m.Misses),
                TotalSets = metrics.Values.Sum(m => m.Sets),
                TotalRemoves = metrics.Values.Sum(m => m.Removes),
                TotalErrors = metrics.Values.Sum(m => m.Errors),
                OverallHitRate = metrics.Values.Sum(m => m.Hits + m.Misses) > 0 
                    ? (double)metrics.Values.Sum(m => m.Hits) / metrics.Values.Sum(m => m.Hits + m.Misses) 
                    : 0,
                CacheTypes = [.. metrics.Keys]
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving cache stats");
            return StatusCode(500, "Error retrieving cache stats");
        }
    }
}

public class CacheStats
{
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public long TotalSets { get; set; }
    public long TotalRemoves { get; set; }
    public long TotalErrors { get; set; }
    public double OverallHitRate { get; set; }
    public List<string> CacheTypes { get; set; } = [];
}

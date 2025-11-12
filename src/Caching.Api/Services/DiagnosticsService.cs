using Caching.Api.Services.Interfaces;
using System.Diagnostics;

namespace Caching.Api.Services;

public class DiagnosticsService(
    ICacheMetricsService metricsService,
    ILogger<DiagnosticsService> logger
)
{
    private readonly Dictionary<string, Stopwatch> _operationTimers = [];

    public void StartOperation(string operationId, string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        _operationTimers[operationId] = stopwatch;
        logger.LogInformation("Starting operation {OperationName} with ID {OperationId}", operationName, operationId);
    }

    public void EndOperation(string operationId, string operationName, string cacheType)
    {
        if (_operationTimers.TryGetValue(operationId, out var stopwatch))
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            _operationTimers.Remove(operationId);
            
            logger.LogInformation("Completed operation {OperationName} with ID {OperationId} in {ElapsedMs}ms for cache type {CacheType}", 
                operationName, operationId, elapsedMs, cacheType);
                
            // Log slow operations
            if (elapsedMs > 100) // Log operations taking more than 100ms
            {
                logger.LogWarning("Slow operation detected: {OperationName} took {ElapsedMs}ms for cache type {CacheType}", 
                    operationName, elapsedMs, cacheType);
            }
        }
    }

    public CacheDiagnostics GetDiagnostics()
    {
        var diagnostics = new CacheDiagnostics
        {
            Timestamp = DateTime.UtcNow,
            Metrics = metricsService.GetAllMetrics(),
            SystemInfo = GetSystemInfo()
        };

        logger.LogInformation("Generated cache diagnostics report");
        return diagnostics;
    }

    public async Task<CacheHealthCheck> PerformHealthCheckAsync(IEnhancedCacheService cacheService)
    {
        var healthCheck = new CacheHealthCheck
        {
            Timestamp = DateTime.UtcNow,
            Status = HealthStatus.Healthy,
            Checks = []
        };

        try
        {
            // Test basic connectivity
            var testKey = $"health-check-{Guid.NewGuid()}";
            var testValue = "health-check-value";
            var policy = new Models.CachePolicy { AbsoluteExpiration = TimeSpan.FromMinutes(1) };

            // Set operation
            var setStart = Stopwatch.StartNew();
            await cacheService.SetAsync(testKey, testValue, policy);
            setStart.Stop();
            healthCheck.Checks.Add(new HealthCheckResult
            {
                Name = "Set Operation",
                Status = HealthStatus.Healthy,
                ResponseTimeMs = setStart.ElapsedMilliseconds,
                Details = "Successfully set test value"
            });

            // Get operation
            var getStart = Stopwatch.StartNew();
            var retrievedValue = await cacheService.GetAsync<string>(testKey);
            getStart.Stop();
            
            if (retrievedValue == testValue)
            {
                healthCheck.Checks.Add(new HealthCheckResult
                {
                    Name = "Get Operation",
                    Status = HealthStatus.Healthy,
                    ResponseTimeMs = getStart.ElapsedMilliseconds,
                    Details = "Successfully retrieved test value"
                });
            }
            else
            {
                healthCheck.Checks.Add(new HealthCheckResult
                {
                    Name = "Get Operation",
                    Status = HealthStatus.Unhealthy,
                    ResponseTimeMs = getStart.ElapsedMilliseconds,
                    Details = "Retrieved value does not match expected value"
                });
                healthCheck.Status = HealthStatus.Degraded;
            }

            // Remove operation
            var removeStart = Stopwatch.StartNew();
            await cacheService.RemoveAsync(testKey);
            removeStart.Stop();
            healthCheck.Checks.Add(new HealthCheckResult
            {
                Name = "Remove Operation",
                Status = HealthStatus.Healthy,
                ResponseTimeMs = removeStart.ElapsedMilliseconds,
                Details = "Successfully removed test value"
            });

            logger.LogInformation("Health check completed with status: {Status}", healthCheck.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed");
            healthCheck.Status = HealthStatus.Unhealthy;
            healthCheck.Checks.Add(new HealthCheckResult
            {
                Name = "Overall Health",
                Status = HealthStatus.Unhealthy,
                Details = $"Health check failed: {ex.Message}"
            });
        }

        return healthCheck;
    }

    private SystemInfo GetSystemInfo()
    {
        return new SystemInfo
        {
            MachineName = Environment.MachineName,
            OSVersion = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet,
            Timestamp = DateTime.UtcNow
        };
    }
}

public class CacheDiagnostics
{
    public DateTime Timestamp { get; set; }
    public IDictionary<string, CacheMetrics> Metrics { get; set; } = new Dictionary<string, CacheMetrics>();
    public SystemInfo SystemInfo { get; set; } = new SystemInfo();
}

public class SystemInfo
{
    public string MachineName { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public long WorkingSet { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CacheHealthCheck
{
    public DateTime Timestamp { get; set; }
    public HealthStatus Status { get; set; }
    public List<HealthCheckResult> Checks { get; set; } = new List<HealthCheckResult>();
}

public class HealthCheckResult
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public long ResponseTimeMs { get; set; }
    public string Details { get; set; } = string.Empty;
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

using Caching.Api.Services.Interfaces;
using System.Collections.Concurrent;

namespace Caching.Api.Services;

public class CacheMetricsService : ICacheMetricsService
{
    private readonly ConcurrentDictionary<string, CacheMetrics> _metrics;
    private readonly Lock _lock = new();

    public CacheMetricsService()
    {
        _metrics = new ConcurrentDictionary<string, CacheMetrics>();
    }

    public void RecordHit(string cacheType)
    {
        var metrics = _metrics.GetOrAdd(cacheType, _ => new CacheMetrics());
        metrics.Hits++;
        metrics.LastHit = DateTime.UtcNow;
    }

    public void RecordMiss(string cacheType)
    {
        var metrics = _metrics.GetOrAdd(cacheType, _ => new CacheMetrics());
        metrics.Misses++;
        metrics.LastMiss = DateTime.UtcNow;
    }

    public void RecordSet(string cacheType)
    {
        var metrics = _metrics.GetOrAdd(cacheType, _ => new CacheMetrics());
        metrics.Sets++;
        metrics.LastSet = DateTime.UtcNow;
    }

    public void RecordRemove(string cacheType)
    {
        var metrics = _metrics.GetOrAdd(cacheType, _ => new CacheMetrics());
        metrics.Removes++;
        metrics.LastRemove = DateTime.UtcNow;
    }

    public void RecordError(string cacheType, string operation)
    {
        var metrics = _metrics.GetOrAdd(cacheType, _ => new CacheMetrics());
        metrics.Errors++;
        metrics.LastError = DateTime.UtcNow;
    }

    public CacheMetrics GetMetrics(string cacheType)
    {
        return _metrics.GetOrAdd(cacheType, _ => new CacheMetrics());
    }

    public void ResetMetrics(string cacheType)
    {
        if (_metrics.ContainsKey(cacheType))
        {
            _metrics[cacheType] = new CacheMetrics();
        }
    }

    public IDictionary<string, CacheMetrics> GetAllMetrics()
    {
        return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void ResetAllMetrics()
    {
        foreach (var key in _metrics.Keys.ToList())
        {
            _metrics[key] = new CacheMetrics();
        }
    }
}

namespace Caching.Api.Services.Interfaces;

public interface ICacheMetricsService
{
    void RecordHit(string cacheType);
    void RecordMiss(string cacheType);
    void RecordSet(string cacheType);
    void RecordRemove(string cacheType);
    void RecordError(string cacheType, string operation);
    CacheMetrics GetMetrics(string cacheType);
    void ResetMetrics(string cacheType);
    IDictionary<string, CacheMetrics> GetAllMetrics();
    void ResetAllMetrics();
}

public class CacheMetrics
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long Sets { get; set; }
    public long Removes { get; set; }
    public long Errors { get; set; }
    public double HitRate => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) : 0;
    
    // Additional metrics
    public DateTime LastHit { get; set; }
    public DateTime LastMiss { get; set; }
    public DateTime LastSet { get; set; }
    public DateTime LastRemove { get; set; }
    public DateTime LastError { get; set; }
}

namespace Caching.Api.Models;

public class CacheEntry<T>
{
    public T? Data { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string SchemaVersion { get; set; } = "1.0";
    public CompatibilityMode CompatibilityMode { get; set; } = CompatibilityMode.Strict;
}

public enum CompatibilityMode
{
    Strict,      // Exact version match required
    Compatible,  // Compatible versions allowed
    Lenient      // Any version allowed
}

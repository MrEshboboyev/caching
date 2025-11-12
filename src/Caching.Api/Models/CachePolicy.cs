namespace Caching.Api.Models;

public class CachePolicy
{
    public TimeSpan? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public CachePriority Priority { get; set; } = CachePriority.Normal;
    public bool UseCompression { get; set; } = false;
    public string Version { get; set; } = "1.0";
    public List<string> Tags { get; set; } = [];
    public SerializationFormat SerializationFormat { get; set; } = SerializationFormat.Json;
}

public enum SerializationFormat
{
    Json,
    MessagePack
}

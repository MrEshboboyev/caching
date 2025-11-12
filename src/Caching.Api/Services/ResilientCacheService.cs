using Caching.Api.Models;
using Caching.Api.Services.Interfaces;
using System.Collections.Concurrent;

namespace Caching.Api.Services;

public class ResilientCacheService(
    IEnhancedCacheService cacheService,
    ILogger<ResilientCacheService> logger,
    int maxRetryAttempts = 3,
    TimeSpan? retryDelay = null
) : IEnhancedCacheService
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new();
    private readonly TimeSpan _retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);

    public async Task<T?> GetAsync<T>(string key)
    {
        var circuitBreaker = GetCircuitBreaker("get");
        if (!circuitBreaker.CanExecute())
        {
            logger.LogWarning("Circuit breaker is open for GET operation, returning default value");
            return default;
        }

        try
        {
            var result = await ExecuteWithRetryAsync(async () => await cacheService.GetAsync<T>(key));
            circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            logger.LogError(ex, "Error retrieving from cache with key: {Key}", key);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, CachePolicy policy)
    {
        var circuitBreaker = GetCircuitBreaker("set");
        if (!circuitBreaker.CanExecute())
        {
            logger.LogWarning("Circuit breaker is open for SET operation, skipping cache set");
            return;
        }

        try
        {
            await ExecuteWithRetryAsync(async () => await cacheService.SetAsync(key, value, policy));
            circuitBreaker.RecordSuccess();
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            logger.LogError(ex, "Error setting cache entry for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key)
    {
        var circuitBreaker = GetCircuitBreaker("remove");
        if (!circuitBreaker.CanExecute())
        {
            logger.LogWarning("Circuit breaker is open for REMOVE operation, skipping cache remove");
            return;
        }

        try
        {
            await ExecuteWithRetryAsync(async () => await cacheService.RemoveAsync(key));
            circuitBreaker.RecordSuccess();
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var circuitBreaker = GetCircuitBreaker("exists");
        if (!circuitBreaker.CanExecute())
        {
            logger.LogWarning("Circuit breaker is open for EXISTS operation, returning false");
            return false;
        }

        try
        {
            var result = await ExecuteWithRetryAsync(async () => await cacheService.ExistsAsync(key));
            circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            logger.LogError(ex, "Error checking existence of cache entry for key: {Key}", key);
            throw;
        }
    }

    public async Task ClearAsync()
    {
        var circuitBreaker = GetCircuitBreaker("clear");
        if (!circuitBreaker.CanExecute())
        {
            logger.LogWarning("Circuit breaker is open for CLEAR operation, skipping cache clear");
            return;
        }

        try
        {
            await ExecuteWithRetryAsync(async () => await cacheService.ClearAsync());
            circuitBreaker.RecordSuccess();
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            logger.LogError(ex, "Error clearing cache");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag)
    {
        var circuitBreaker = GetCircuitBreaker("getkeys");
        if (!circuitBreaker.CanExecute())
        {
            logger.LogWarning("Circuit breaker is open for GETKEYS operation, returning empty collection");
            return Enumerable.Empty<string>();
        }

        try
        {
            var result = await ExecuteWithRetryAsync(async () => await cacheService.GetKeysByTagAsync(tag));
            circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            logger.LogError(ex, "Error retrieving keys by tag: {Tag}", tag);
            throw;
        }
    }

    public async Task RemoveByTagAsync(string tag)
    {
        var circuitBreaker = GetCircuitBreaker("removetags");
        if (!circuitBreaker.CanExecute())
        {
            logger.LogWarning("Circuit breaker is open for REMOVETAGS operation, skipping remove by tag");
            return;
        }

        try
        {
            await ExecuteWithRetryAsync(async () => await cacheService.RemoveByTagAsync(tag));
            circuitBreaker.RecordSuccess();
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            logger.LogError(ex, "Error removing entries by tag: {Tag}", tag);
            throw;
        }
    }

    private CircuitBreaker GetCircuitBreaker(string operation)
    {
        return _circuitBreakers.GetOrAdd(operation, _ => new CircuitBreaker(
            failureThreshold: 5,
            recoveryTimeout: TimeSpan.FromMinutes(1),
            logger: logger));
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= maxRetryAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                // Don't retry on certain exceptions
                if (ex is OperationCanceledException || ex is OutOfMemoryException)
                {
                    throw;
                }
                
                if (attempt == maxRetryAttempts)
                {
                    throw new CacheOperationException($"Operation failed after {maxRetryAttempts} attempts", ex);
                }
                
                logger.LogWarning(ex, "Attempt {Attempt} failed, retrying in {Delay}", attempt + 1, _retryDelay);
                await Task.Delay(_retryDelay);
            }
        }
        
        throw new CacheOperationException($"Operation failed after {maxRetryAttempts} attempts", lastException!);
    }

    private async Task ExecuteWithRetryAsync(Func<Task> operation)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= maxRetryAttempts; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                // Don't retry on certain exceptions
                if (ex is OperationCanceledException || ex is OutOfMemoryException)
                {
                    throw;
                }
                
                if (attempt == maxRetryAttempts)
                {
                    throw new CacheOperationException($"Operation failed after {maxRetryAttempts} attempts", ex);
                }
                
                logger.LogWarning(ex, "Attempt {Attempt} failed, retrying in {Delay}", attempt + 1, _retryDelay);
                await Task.Delay(_retryDelay);
            }
        }
        
        throw new CacheOperationException($"Operation failed after {maxRetryAttempts} attempts", lastException!);
    }
}

public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _recoveryTimeout;
    private readonly ILogger _logger;
    private readonly object _lock = new object();
    
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitState _state;

    public CircuitBreaker(int failureThreshold, TimeSpan recoveryTimeout, ILogger logger)
    {
        _failureThreshold = failureThreshold;
        _recoveryTimeout = recoveryTimeout;
        _logger = logger;
        _state = CircuitState.Closed;
    }

    public bool CanExecute()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return true;
                    
                case CircuitState.Open:
                    // Check if we should transition to Half-Open
                    if (DateTime.UtcNow >= _lastFailureTime + _recoveryTimeout)
                    {
                        _state = CircuitState.HalfOpen;
                        _logger.LogInformation("Circuit breaker transitioning to Half-Open state");
                        return true;
                    }
                    return false;
                    
                case CircuitState.HalfOpen:
                    // Allow one test request
                    return true;
                    
                default:
                    return true;
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            if (_state != CircuitState.Closed)
            {
                _logger.LogInformation("Circuit breaker transitioning to Closed state");
            }
            _state = CircuitState.Closed;
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            if (_state == CircuitState.HalfOpen || _failureCount >= _failureThreshold)
            {
                if (_state != CircuitState.Open)
                {
                    _logger.LogWarning("Circuit breaker transitioning to Open state after {FailureCount} failures", _failureCount);
                }
                _state = CircuitState.Open;
            }
        }
    }
}

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public class CacheOperationException(
    string message,
    Exception? innerException
) : Exception(message, innerException)
{
}

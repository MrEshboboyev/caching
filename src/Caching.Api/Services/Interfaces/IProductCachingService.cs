using Caching.Api.Models;

namespace Caching.Api.Services.Interfaces;

public interface IProductCachingService
{
    Task<IEnumerable<Product>> GetProductsAsync();
    Task<IEnumerable<Product>> GetProductsWithSlidingExpirationAsync();
    Task<IEnumerable<Product>> GetProductsWithPostEvictionCallbackAsync();
    Task<IEnumerable<Product>> GetProductsWithDependencyAsync();
}

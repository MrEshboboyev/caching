using Caching.Api.Models;

namespace Caching.Api.Services;

public interface IProductService
{
    IEnumerable<Product> GetAll();
}

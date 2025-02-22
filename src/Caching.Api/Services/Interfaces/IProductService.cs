using Caching.Api.Models;

namespace Caching.Api.Services.Interfaces;

public interface IProductService
{
    IEnumerable<Product> GetAll();
}

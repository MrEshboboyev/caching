using Caching.Api.Models;

namespace Caching.Api.Services;

public class ProductService : IProductService
{
    public IEnumerable<Product> GetAll()
    {
        Thread.Sleep(2000); // simulate slow db query

        return
        [
            new Product { Id = 1, Name = "Keyboard", Price = 20 },
            new Product { Id = 2, Name = "Mouse", Price = 10 },
            new Product { Id = 3, Name = "Monitor", Price = 100 }
        ];
    }
}

using Catalog.API.Entities;
using System.Linq.Expressions;

namespace Catalog.API.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetProductsAsync();

        Task<IEnumerable<Product>> GetProductsAsync(Expression<Func<Product, bool>> filter);

        Task<Product> GetProductAsync(string id);

        Task<Product> GetProductAsync(Expression<Func<Product, bool>> filter);

        Task<IEnumerable<Product>> GetProductByNameAsync(string name);

        Task<IEnumerable<Product>> GetProductByCategoryAsync(string categoryName);

        Task CreateProductAsync(Product product);

        Task<bool> UpdateProductAsync(Product product);

        Task<bool> DeleteProductAsync(string id);
    }
}

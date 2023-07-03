using Basket.API.Entities;

namespace Basket.API.Repositories
{
    public interface IBasketRepository
    {
        Task<ShoppingCart> GetBasketAsync(string username);
        Task<ShoppingCart> UpdateBasketAsync(ShoppingCart basket);
        Task DeleteBasketAsync(string username);
    }
}

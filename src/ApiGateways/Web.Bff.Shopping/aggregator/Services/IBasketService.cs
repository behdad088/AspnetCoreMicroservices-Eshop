using Web.Shopping.HttpAggregator.Models;

namespace Web.Shopping.HttpAggregator.Services
{
    public interface IBasketService
    {
        Task<BasketModel> GetBasketAsync(string username);
    }
}

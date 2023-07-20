using Web.Shopping.HttpAggregator.Models;

namespace Web.Shopping.HttpAggregator.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderResponseModel>> GetOrdersByUsernameAsync(string userName);
    }
}

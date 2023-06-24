using Ordering.Domain.Entities;

namespace Ordering.Application.Contracts.Persistence
{
    public interface IOrderRepository : IAsyncRepository<Order>
    {
        Task<IEnumerable<Order>> GetOrdersByUserNameAsync(string userName);
    }
}

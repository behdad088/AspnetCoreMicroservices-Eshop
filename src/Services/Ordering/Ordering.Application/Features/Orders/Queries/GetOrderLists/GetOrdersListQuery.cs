using MediatR;

namespace Ordering.Application.Features.Orders.Queries.GetOrderLists
{
    public class GetOrdersListQuery : IRequest<List<OrdersVm>>
    {
        public string Username { get; set; }

        public GetOrdersListQuery(string username)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
        }
    }
}

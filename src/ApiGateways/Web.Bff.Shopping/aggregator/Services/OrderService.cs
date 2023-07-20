using AutoMapper;
using Grpc.Core;
using Order.Grpc.Protos;
using Web.Shopping.HttpAggregator.Models;

namespace Web.Shopping.HttpAggregator.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderProtoService.OrderProtoServiceClient _orderServiceClient;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        public OrderService(OrderProtoService.OrderProtoServiceClient orderServiceClient, IMapper mapper, ILogger<OrderService> logger)
        {
            _orderServiceClient = orderServiceClient ?? throw new ArgumentNullException(nameof(orderServiceClient));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<OrderResponseModel>> GetOrdersByUsernameAsync(string userName)
        {
            var orders = await _orderServiceClient.GetOrdersAsync(new GetOredrsRequest() { Username = userName });
            if (orders == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Order not found."));

            var result = _mapper.Map<List<OrderResponseModel>>(orders.Orders);
            return result;
        }
    }
}

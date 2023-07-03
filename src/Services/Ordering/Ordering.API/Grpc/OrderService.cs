using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Order.Grpc.Protos;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Application.Features.Orders.Commands.DeleteOrder;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrderLists;

namespace Ordering.API.Grpc
{
    public class OrderService : OrderProtoService.OrderProtoServiceBase
    {
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IMapper mapper, IMediator mediator, ILogger<OrderService> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<GetOredrsResponse> GetOrders(GetOredrsRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Getting orders for user by Username:{Username}", request.UserName);
            var query = new GetOrdersListQuery(request.UserName);
            var orders = await _mediator.Send(query);
            var orderViewModel = _mapper.Map<List<OrderViewModel>>(orders);
            var result = new GetOredrsResponse();
            result.Orders.AddRange(orderViewModel);
            return result;
        }

        public override async Task<OrderId> CheckoutOrder(CheckoutViewModel request, ServerCallContext context)
        {
            _logger.LogInformation("checkout orders for user by Username:{Username}", request.UserName);
            var checkoutOrderCommand = _mapper.Map<CheckoutOrderCommand>(request);
            var orderId = await _mediator.Send(checkoutOrderCommand);
            var result = new OrderId
            {
                Id = orderId
            };

            return result;
        }

        public override async Task<Empty> UpdateOrder(OrderViewModel request, ServerCallContext context)
        {
            _logger.LogInformation("Update order for user by Username:{Username}", request.UserName);
            var updateOrderCommand = _mapper.Map<UpdateOrderCommand>(request);
            await _mediator.Send(updateOrderCommand);
            return new Empty();
        }

        public override async Task<Empty> DeleteOrder(OrderId request, ServerCallContext context)
        {
            _logger.LogInformation("Delete order by OrderId:{OrderId}", request.Id);
            var command = new DeleteOrderCommand() { Id = request.Id };
            await _mediator.Send(command);
            return new Empty();
        }
    }
}

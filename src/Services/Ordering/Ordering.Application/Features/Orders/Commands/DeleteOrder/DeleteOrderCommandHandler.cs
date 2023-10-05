using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Exceptions;
using Ordering.Domain.Entities;

namespace Ordering.Application.Features.Orders.Commands.DeleteOrder
{
    public record DeleteOrderCommand(int? Id) : IRequest;

    public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<DeleteOrderCommandHandler> _logger;

        public DeleteOrderCommandHandler(IOrderRepository orderRepository, ILogger<DeleteOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
        {
            if (!request.Id.HasValue)
                throw new Exception("Request Id cannot be null.");

            var orderToDelete = await _orderRepository.GetByIdAsync(request.Id.Value);
            if (orderToDelete == null)
                throw new NotFoundException(nameof(Order), request.Id);

            await _orderRepository.DeleteAsync(orderToDelete);
            _logger.LogInformation("Order {OrderId} is successfully deleted.", orderToDelete.Id);
        }
    }
}

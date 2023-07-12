using Eshop.BuildingBlocks.EventBus.RabbitMQ.Event;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Types;

namespace Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions
{
    public interface IRabbitMQConsumer : IDisposable
    {
        Task SubscribeAsync(Func<IntegrationEvent, Task<ActMode>> func, CancellationToken cancellationToken);

        Task<string> SubscribeAsync(Func<IntegrationEvent, Task<ActMode>> func);

        Task UnsubscribeAsync(string consumerTag);
    }
}

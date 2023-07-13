using Eshop.BuildingBlocks.EventBus.RabbitMQ.Event;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Types;

namespace Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions
{
    public interface IRabbitMQConsumer<T> : IDisposable
    {
        Task SubscribeAsync(Func<IntegrationEvent, Task<ActMode>> func, CancellationToken cancellationToken, ushort prefetchCount = 50);

        Task<string> SubscribeAsync(ushort prefetchCount, Func<IntegrationEvent, Task<ActMode>> func);

        Task UnsubscribeAsync(string consumerTag);
    }
}

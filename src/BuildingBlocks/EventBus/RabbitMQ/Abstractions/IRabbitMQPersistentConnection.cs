using RabbitMQ.Client;

namespace Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions
{
    public interface IRabbitMQPersistentConnection : IDisposable
    {
        Task DeclareExchangeAsync(string name, string type, bool durable, bool autoDelete, IDictionary<string, object>? arguments);

        Task DeclareQueueAsync(string name, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object>? arguments);

        Task BindQueueAsync(string queueName, string exchangeName, string routingKey, IDictionary<string, object>? arguments);

        Task<IModel> CreateModelAsync();

        bool IsConnected();
    }
}


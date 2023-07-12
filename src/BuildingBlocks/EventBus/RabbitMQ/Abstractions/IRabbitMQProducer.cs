namespace Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions
{
    public interface IRabbitMQProducer : IDisposable
    {
        Task PublishAsJsonAsync(string routingKey, object obj);

        Task PublishStringAsync(string routingKey, string message);
    }
}

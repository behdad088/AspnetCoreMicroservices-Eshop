namespace Eshop.BuildingBlocks.EventBus.RabbitMQ.Exceptions
{
    public class RabbitMQConsumerException : Exception
    {
    }

    public class RabbitMQConsumerIdIsNullException : RabbitMQConsumerException { }
}

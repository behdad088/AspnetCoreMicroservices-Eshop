namespace Eshop.BuildingBlocks.EventBus.RabbitMQ.Types
{
    public enum ActMode
    {
        Ack,
        NackWithoutRequeue,
        NackAndRequeue,
    }
}

using NodaTime;
using System.Text.Json.Serialization;

namespace Eshop.BuildingBlocks.EventBus.RabbitMQ.Event
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime());
        }

        [JsonConstructor]
        public IntegrationEvent(Guid id, Instant createDate)
        {
            Id = id;
            CreationDate = createDate;
        }

        [JsonConstructor]
        public IntegrationEvent(Instant? creationDate, string routingKey, string? messageId, string? contentType, byte[] body)
        {
            CreationDate = creationDate;
            RoutingKey = routingKey;
            MessageId = messageId;
            ContentType = contentType;
            Body = body;
        }

        [JsonInclude]
        public Guid Id { get; private init; }

        [JsonInclude]
        public Instant? CreationDate { get; private init; }

        [JsonInclude]
        public string RoutingKey { get; private init; }

        [JsonInclude]
        public string? MessageId { get; private init; }

        [JsonInclude]
        public string? ContentType { get; private init; }

        [JsonInclude]
        public byte[] Body { get; private init; }
    }
}

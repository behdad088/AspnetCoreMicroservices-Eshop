using Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Event;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Types;
using MediatR;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using System.Text;

namespace Ordering.API.EventBusConsumer
{
    public class BasketCheckoutConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BasketCheckoutConsumer> _logger;
        private readonly IRabbitMQConsumer<BasketCheckoutConsumer> _rabbitMQConsumer;

        public BasketCheckoutConsumer(IServiceScopeFactory serviceScopeFactory, ILogger<BasketCheckoutConsumer> logger, IRabbitMQConsumer<BasketCheckoutConsumer> rabbitMQConsumer)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rabbitMQConsumer = rabbitMQConsumer ?? throw new ArgumentNullException(nameof(rabbitMQConsumer));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            await _rabbitMQConsumer.SubscribeAsync(async message =>
            {
                return await HandleEvent(message);
            }, stoppingToken);
        }

        public async Task<ActMode> HandleEvent(IntegrationEvent eventMessage)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            CheckoutOrderCommand? command;

            try
            {
                var body = Encoding.UTF8.GetString(eventMessage.Body);
                command = JsonConvert.DeserializeObject<CheckoutOrderCommand>(body, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));

                if (command == null)
                    return ActMode.Ack;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Something went wrong when deserialize message body with {Id}", eventMessage.MessageId);
                return ActMode.Ack;
            }

            try
            {
                var result = await mediator.Send(command);
                _logger.LogInformation("Order checkout was processed successfully for user {Username} with orderId {OrderId}", command.Username, result);
                return ActMode.Ack;
            }
            catch (Exception e)
            {
                // TODO: Create a queue to pass all the failed messages so no message is lost
                _logger.LogWarning(e, "Something went wrong when processing checkout order for username {Username}", command.Username);
                return ActMode.Ack;
            }
        }
    }
}
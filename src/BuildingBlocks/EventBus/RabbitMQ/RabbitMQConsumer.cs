using Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Event;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Types;
using Microsoft.Extensions.Logging;
using NodaTime;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Eshop.BuildingBlocks.EventBus.RabbitMQ
{
    public class RabbitMQConsumer : IRabbitMQConsumer
    {
        private readonly IRabbitMQPersistentConnection _rabbitMQPersistentConnection;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private readonly string _queueName;
        private IModel Channel;
        private bool IsQueueDeclared = false;
        private readonly SemaphoreSlim _channelSemaphore;

        public RabbitMQConsumer(
            IRabbitMQPersistentConnection rabbitMQPersistentConnection,
            string service,
            string environment,
            string consumerName,
            ILogger<RabbitMQConsumer> logger)
        {
            if (string.IsNullOrEmpty(service)) throw new ArgumentNullException(nameof(service));
            if (string.IsNullOrEmpty(environment)) throw new ArgumentNullException(nameof(environment));
            if (string.IsNullOrEmpty(consumerName)) throw new ArgumentNullException(nameof(consumerName));

            _rabbitMQPersistentConnection = rabbitMQPersistentConnection ?? throw new ArgumentNullException(nameof(rabbitMQPersistentConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueName = $"{environment}.queue.{service}.{consumerName}";
            _channelSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        }

        public async Task SubscribeAsync(Func<IntegrationEvent, Task<ActMode>> func, CancellationToken cancellationToken)
        {
            string? consumerTag = null;
            int retryForAvailability = 0;
            const int MAX_RETRIES = 60;
            const int ONE_MINUTE_SLEEP = 10;
            const int INFINITE_SLEEP = -1;

            while (consumerTag == null && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    consumerTag = await SubscribeAsync(func);
                }
                catch (Exception)
                {
                    if (retryForAvailability < MAX_RETRIES)
                    {
                        retryForAvailability++;
                        await TaskDelayAsync(ONE_MINUTE_SLEEP, cancellationToken);
                    }
                    else
                        throw;
                }

                if (consumerTag != null)
                {
                    //https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.delay?view=net-7.0  -1 will delay infinite
                    await TaskDelayAsync(INFINITE_SLEEP, cancellationToken);
                    await UnsubscribeAsync(consumerTag: consumerTag);
                }
            }
        }

        public async Task<string> SubscribeAsync(Func<IntegrationEvent, Task<ActMode>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            await DeclareQueueAsync();

            Channel = await GetChannelAsync();

            try
            {
                var consumer = new AsyncEventingBasicConsumer(Channel);
                consumer.Received += async (sender, e) => await OnMessageReceivedAsync(sender, e, func);
                consumer.Registered += OnRegisteredAsync;
                consumer.ConsumerCancelled += OnConsumerCancelledAsync;
                consumer.Unregistered += OnUnregisteredAsync;

                var consumerTag = Channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer, exclusive: false);
                return consumerTag;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "An unexpected exception happened when subscribing to queue {QueueName}.", _queueName);
                Channel.Close(200, $"Unhandled exception in {nameof(SubscribeAsync)}.");
                Channel.Dispose();
                throw;
            }
        }

        public async Task UnsubscribeAsync(string consumerTag)
        {
            Channel?.Close(200, $"Unsubscribe {consumerTag}");
            Channel?.Dispose();
            await Task.CompletedTask;
        }

        private async Task<IModel> GetChannelAsync()
        {
            if (Channel == null)
            {
                await _channelSemaphore.WaitAsync();

                try
                {
                    if (Channel == null)
                    {
                        Channel = await _rabbitMQPersistentConnection.CreateModelAsync();
                        if (Channel == null) throw new InvalidOperationException($"Something went wrong creating channel for queue {_queueName}");
                        Channel.CallbackException += OnCallbackException;
                        Channel.ModelShutdown += OnModelShutDown;
                    }
                }
                finally
                {
                    _channelSemaphore.Release();
                }
            }

            return Channel;
        }

        private async Task DeclareQueueAsync()
        {
            if (!IsQueueDeclared)
            {
                await _channelSemaphore.WaitAsync();
                try
                {
                    if (!IsQueueDeclared)
                    {
                        await _rabbitMQPersistentConnection.DeclareQueueAsync(name: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                        IsQueueDeclared = true;
                    }
                }
                finally
                {
                    _channelSemaphore.Release();
                }
            }
        }

        private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
        {
            _logger.LogError("A callback RabbitMQ connection throw exception for queue {QueueName} with exception {Exception}", _queueName, e.Exception.Message);
        }

        private void OnModelShutDown(object? sender, ShutdownEventArgs e)
        {
            _logger.LogError("A RabbitMQ channel for queue {QueueName} was shut down. The connection is open {IsConnectionOpen} ", _queueName, _rabbitMQPersistentConnection.IsConnected());
        }

        private async Task OnConsumerCancelledAsync(object sender, ConsumerEventArgs e)
        {
            _logger?.LogInformation("ConsumerCancelled event for queue {QueueName} with ConsumerTags: {Value}", _queueName, string.Join(", ", e.ConsumerTags));
            await Task.CompletedTask;
        }

        private async Task OnRegisteredAsync(object sender, ConsumerEventArgs e)
        {
            _logger?.LogInformation("Registered event for queue {QueueName} with ConsumerTags: {Value}", _queueName, string.Join(", ", e.ConsumerTags));
            await Task.CompletedTask;
        }

        private async Task OnUnregisteredAsync(object sender, ConsumerEventArgs e)
        {
            _logger?.LogInformation("Unregistered event for queue {QueueName} with ConsumerTags: {Value}", _queueName, string.Join(", ", e.ConsumerTags));
            await Task.CompletedTask;
        }

        private IntegrationEvent GetIntegrationEventObject(BasicDeliverEventArgs e)
        {
            var message = new IntegrationEvent(
                        body: e.Body.ToArray(),
                        routingKey: e.RoutingKey,
                        contentType: e.BasicProperties.IsContentTypePresent() ? e.BasicProperties.ContentType : null,
                        creationDate: e.BasicProperties.IsTimestampPresent() ? Instant.FromUnixTimeSeconds(e.BasicProperties.Timestamp.UnixTime) : null,
                        messageId: e.BasicProperties.IsMessageIdPresent() ? e.BasicProperties.MessageId : null);

            return message;
        }

        private void AcknowledgeMessage(ActMode actMode, ulong deliveryTag)
        {
            switch (actMode)
            {
                case ActMode.Ack:
                    Channel.BasicAck(deliveryTag, multiple: false);
                    break;
                case ActMode.NackWithoutRequeue:
                    Channel.BasicNack(deliveryTag, multiple: false, requeue: false);
                    break;
                case ActMode.NackAndRequeue:
                    Channel.BasicNack(deliveryTag, multiple: false, requeue: true);
                    break;
                default:
                    throw new Exception("Unexpected acknowledgement mode.");
            }
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs e, Func<IntegrationEvent, Task<ActMode>> func)
        {
            var message = GetIntegrationEventObject(e);
            ActMode actMode;

            try
            {
                actMode = await func(message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unhandled exception in handler for queue {QueneName}", _queueName);
                actMode = ActMode.NackWithoutRequeue;
            }

            AcknowledgeMessage(actMode, e.DeliveryTag);
        }

        private async Task TaskDelayAsync(int time, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(time), cancellationToken);
            }
            catch (TaskCanceledException) { }
        }

        public void Dispose()
        {
            if (Channel != null)
            {
                Channel?.Close(200, $"Disposing {nameof(RabbitMQPersistentConnection)}");
                Channel?.Dispose();
            }
        }
    }
}

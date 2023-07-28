using Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Event;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Exceptions;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Types;
using Microsoft.Extensions.Logging;
using NodaTime;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Sockets;

namespace Eshop.BuildingBlocks.EventBus.RabbitMQ
{
    public class RabbitMQConsumer<T> : IRabbitMQConsumer<T>
    {
        private readonly IRabbitMQPersistentConnection _rabbitMQPersistentConnection;
        private readonly ILogger<RabbitMQConsumer<T>> _logger;
        private readonly string _queueName;
        private IModel? Channel;
        private bool IsQueueDeclared = false;
        private readonly SemaphoreSlim _channelSemaphore;

        public RabbitMQConsumer(
            IRabbitMQPersistentConnection rabbitMQPersistentConnection,
            string service,
            string environment,
            string consumerName,
            ILogger<RabbitMQConsumer<T>> logger)
        {
            if (string.IsNullOrEmpty(service)) throw new ArgumentNullException(nameof(service));
            if (string.IsNullOrEmpty(environment)) throw new ArgumentNullException(nameof(environment));
            if (string.IsNullOrEmpty(consumerName)) throw new ArgumentNullException(nameof(consumerName));

            _rabbitMQPersistentConnection = rabbitMQPersistentConnection ?? throw new ArgumentNullException(nameof(rabbitMQPersistentConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueName = $"{environment}.queue.{service}.{consumerName}";
            _channelSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        }

        public async Task SubscribeAsync(Func<IntegrationEvent, Task<ActMode>> func, CancellationToken cancellationToken, ushort prefetchCount = 50)
        {
            string? consumerTag = null;
            const int MAX_RETRIES = 5;
            const int INFINITE_SLEEP = -1;

            try
            {
                var policy = Policy.Handle<SocketException>()
                            .Or<RabbitMQConsumerIdIsNullException>()
                            .WaitAndRetryAsync(MAX_RETRIES, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                            {
                                _logger.LogWarning(ex, "RabbitMQ consumer could not subscribe after {TimeOut}s for event {EventName}", $"{time.TotalSeconds:n1}", typeof(T).Name);
                            }
                    );

                await policy.ExecuteAsync(async () =>
                {
                    consumerTag = await SubscribeAsync(prefetchCount, func); ;

                    if (consumerTag == null)
                        throw new RabbitMQConsumerIdIsNullException();

                    //https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.delay?view=net-7.0  -1 will delay infinite
                    await TaskDelayAsync(INFINITE_SLEEP, cancellationToken);
                    await UnsubscribeAsync(consumerTag: consumerTag);
                });

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Something unexpected happened when tying to subscribe to event {EventName}", typeof(T).Name);
                throw;
            }
        }

        public async Task<string> SubscribeAsync(ushort prefetchCount, Func<IntegrationEvent, Task<ActMode>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            await DeclareQueueAsync();

            Channel = await GetChannelAsync(prefetchCount);

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
            _logger.LogInformation("Unsubscribe to consumer tag {ConsumerTag}", consumerTag);
            Channel?.Close(200, $"Unsubscribe {consumerTag}");
            Channel?.Dispose();
            await Task.CompletedTask;
        }

        private async Task<IModel> GetChannelAsync(ushort prefetchCount)
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

                        Channel.BasicQos(prefetchSize: 0, prefetchCount: prefetchCount, global: false);
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

        private static IntegrationEvent GetIntegrationEventObject(BasicDeliverEventArgs e)
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
            if (Channel == null) throw new Exception($"channel cannot be wrong for queue {_queueName}");

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
                _logger?.LogError(ex, "Unhandled exception in handler for queue {QueueName}", _queueName);
                actMode = ActMode.NackWithoutRequeue;
            }

            AcknowledgeMessage(actMode, e.DeliveryTag);
        }

        private static async Task TaskDelayAsync(int time, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(time, cancellationToken);
            }
            catch (TaskCanceledException) { }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Channel != null)
                {
                    Channel?.Close(200, $"Disposing {nameof(RabbitMQPersistentConnection)}");
                    Channel?.Dispose();
                }
            }
        }

    }
}

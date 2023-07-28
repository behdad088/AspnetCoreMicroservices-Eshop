using Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace Eshop.BuildingBlocks.EventBus.RabbitMQ
{
    public class RabbitMQPersistentConnection : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMQPersistentConnection> _logger;
        private readonly int _retryCount;
        private IConnection? _connection;
        public bool Disposed;
        private readonly SemaphoreSlim _sessionSemaphore;

        public RabbitMQPersistentConnection(IConnectionFactory connectionFactory, ILogger<RabbitMQPersistentConnection> logger, int retryCount = 5)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryCount = retryCount;
            _sessionSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        }

        public async Task DeclareExchangeAsync(string name, string type, bool durable, bool autoDelete, IDictionary<string, object>? arguments)
        {
            using var channel = await CreateModelAsync();
            channel.ExchangeDeclare(exchange: name, type: type, durable: durable, autoDelete: autoDelete, arguments: arguments);
            _logger?.LogInformation("RMQ exchange {ExchangeName} is declared.", name);
        }

        public async Task DeclareQueueAsync(string name, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object>? arguments)
        {
            using var channel = await CreateModelAsync();
            var response = channel.QueueDeclare(queue: name, durable: durable, exclusive: exclusive, autoDelete: autoDelete, arguments: arguments);
            _logger?.LogInformation("RMQ queue {QueueName} is declared. It has {Value1} messages and {Value2} consumers.", name, response.MessageCount, response.ConsumerCount);
        }

        public async Task BindQueueAsync(string queueName, string exchangeName, string routingKey, IDictionary<string, object>? arguments)
        {
            using var channel = await CreateModelAsync();
            channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey, arguments: arguments);
            _logger?.LogInformation("RMQ queue {QueueName} is bound to exchange {ExchangeName} by routing key {RoutingKey}.", queueName, exchangeName, routingKey);
        }

        public async Task<IModel> CreateModelAsync()
        {
            if (!IsConnected())
                await TryConnectAsync();

            if (!IsConnected())
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");

            return _connection?.CreateModel() ?? throw new Exception("Something went wrong when creating rmq channel.");
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

                if (Disposed || _connection == null) return;

                Disposed = true;
                try
                {
                    _connection.ConnectionShutdown -= OnConnectionShutdownAsync;
                    _connection.CallbackException -= OnCallbackExceptionAsync;
                    _connection.ConnectionBlocked -= OnConnectionBlockedAsync;
                    _connection?.Close(200, $"Dispose of {nameof(RabbitMQPersistentConnection)}");
                    _connection?.Dispose();
                }
                catch (IOException ex)
                {
                    _logger.LogCritical(ex, "Something went wrong disposing rmq connection.");
                }
            }
        }

        protected async Task<bool> TryConnectAsync()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect");

            try
            {
                await _sessionSemaphore.WaitAsync();
                var policy = Policy.Handle<SocketException>()
                        .Or<BrokerUnreachableException>()
                        .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                        {
                            _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s", $"{time.TotalSeconds:n1}");
                        }
                    );

                policy.Execute(() =>
                {
                    _connection = _connectionFactory
                            .CreateConnection();
                });

                if (IsConnected())
                {
                    if (_connection == null) throw new Exception("Something went wrong creating rmq connection.");

                    _connection.ConnectionShutdown += OnConnectionShutdownAsync;
                    _connection.CallbackException += OnCallbackExceptionAsync;
                    _connection.ConnectionBlocked += OnConnectionBlockedAsync;
                    _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);
                    return true;
                }
                else
                {
                    _logger.LogCritical("Fatal error: RabbitMQ connections could not be created and opened");
                    return false;
                }
            }
            finally
            {
                _sessionSemaphore.Release();
            }
        }

        private async void OnConnectionBlockedAsync(object? sender, ConnectionBlockedEventArgs e)
        {
            if (Disposed) return;

            _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            await TryConnectAsync();
        }

        private async void OnCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
        {
            if (Disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            await TryConnectAsync();
        }

        private async void OnConnectionShutdownAsync(object? sender, ShutdownEventArgs reason)
        {
            if (Disposed) return;

            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            await TryConnectAsync();
        }

        public bool IsConnected()
        {
            return _connection is { IsOpen: true } && !Disposed;
        }
    }
}

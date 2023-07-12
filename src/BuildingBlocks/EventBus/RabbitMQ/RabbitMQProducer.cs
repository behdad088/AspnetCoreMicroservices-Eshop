using Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NodaTime;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Eshop.BuildingBlocks.EventBus.RabbitMQ
{
    public class RabbitMQProducer : IRabbitMQProducer
    {
        private readonly string _exchangeName;
        private readonly string _exchangeType;
        private readonly IRabbitMQPersistentConnection _rabbitMQPersistentConnection;
        private readonly SemaphoreSlim _channelSemaphore;
        private IModel Channel;
        private readonly ILogger<RabbitMQProducer> _logger;
        private readonly IClock _clock;
        private bool IsExchangeDeclared = false;

        public RabbitMQProducer(
            IRabbitMQPersistentConnection rabbitMQPersistentConnection,
            ILogger<RabbitMQProducer> logger,
            IClock clock,
            string service,
            string environment,
            string name,
            string exchangeType = "topic")
        {
            AssertRMQExchangeName(service, environment, name, exchangeType);

            _rabbitMQPersistentConnection = rabbitMQPersistentConnection ?? throw new ArgumentNullException(nameof(rabbitMQPersistentConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeName = $"{environment}.exchange.{exchangeType}.{service}.{name}";
            _exchangeType = exchangeType;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _channelSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        }

        public async Task PublishAsJsonAsync(string routingKey, object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var body = Encoding.UTF8.GetBytes(json);
            await PublishAsync(body, routingKey, _clock.GetCurrentInstant(), contentType: "application/json");
        }

        public async Task PublishStringAsync(string routingKey, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            await PublishAsync(body, routingKey, _clock.GetCurrentInstant());
        }


        private async Task PublishAsync(byte[]? body, string routingKey, Instant instant, bool persistent = true, string? contentType = null)
        {
            await DeclareExchange();
            var channel = await GetChannel();
            var properties = channel.CreateBasicProperties();

            properties.MessageId = Guid.NewGuid().ToString();
            properties.Persistent = persistent;
            properties.Timestamp = new AmqpTimestamp(instant.ToUnixTimeSeconds());
            properties.ContentType = contentType;

            await _channelSemaphore.WaitAsync();
            try
            {
                channel.BasicPublish(exchange: _exchangeName, routingKey: routingKey, mandatory: false, basicProperties: properties, body: body);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _channelSemaphore.Release();
            }
        }

        public async Task<IModel> GetChannel()
        {
            if (Channel == null)
            {
                await _channelSemaphore.WaitAsync();
                if (Channel == null)
                {
                    Channel = await _rabbitMQPersistentConnection.CreateModelAsync();
                    if (Channel == null) throw new InvalidOperationException($"Something went wrong creating channel for exchange {_exchangeName}");
                    Channel.CallbackException += OnCallbackException;
                    Channel.ModelShutdown += OnModelShutDown;
                }
                _channelSemaphore.Release();
            }

            return Channel;
        }

        public async Task DeclareExchange()
        {
            if (!IsExchangeDeclared)
            {
                await _channelSemaphore.WaitAsync();
                try
                {
                    if (!IsExchangeDeclared)
                    {
                        await _rabbitMQPersistentConnection.DeclareExchangeAsync(name: _exchangeName, type: _exchangeType, durable: true, autoDelete: false, arguments: null);
                        IsExchangeDeclared = true;
                    }
                }
                finally
                {
                    _channelSemaphore.Release();
                }
            }
        }

        private void AssertRMQExchangeName(string service,
            string environment,
            string name,
            string exchangeType)
        {
            if (string.IsNullOrEmpty(service)) throw new ArgumentNullException(nameof(service));
            if (string.IsNullOrEmpty(environment)) throw new ArgumentNullException(nameof(environment));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(exchangeType)) throw new ArgumentNullException(nameof(exchangeType));
            AssertRMQExchangeType(exchangeType);
        }

        private void AssertRMQExchangeType(string exchangeType)
        {
            if (exchangeType != "topic" && exchangeType != "direct" && exchangeType != "fanout " && exchangeType != "headers ")
                throw new ArgumentException($"Invalid {nameof(exchangeType)} with value {exchangeType}");
        }

        private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
        {
            _logger.LogError("A callback RabbitMQ connection throw exception for exchange {ExchangeName} with exception {Exception}", _exchangeName, e.Exception.Message);
        }

        private void OnModelShutDown(object? sender, ShutdownEventArgs e)
        {
            _logger.LogError("A RabbitMQ channel for exchange {ExchangeName} was shut down. The connection is open {IsConnectionOpen} ", _exchangeName, _rabbitMQPersistentConnection.IsConnected());
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

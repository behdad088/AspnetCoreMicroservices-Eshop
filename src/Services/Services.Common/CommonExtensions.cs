using Eshop.BuildingBlocks.EventBus.RabbitMQ;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NodaTime;
using RabbitMQ.Client;

namespace Services.Common
{
    public static class CommonExtensions
    {
        public static IServiceCollection AddRMQConnection(this IServiceCollection services, string connectionString)
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(connectionString),
                DispatchConsumersAsync = true
            };

            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RabbitMQPersistentConnection>>();
                return new RabbitMQPersistentConnection(factory, logger);
            });

            return services;
        }

        public static IServiceCollection AddRMQConsumer<T>(this IServiceCollection services, string environment, string service, string consumerName)
        {
            services.AddSingleton<IRabbitMQConsumer<T>>(sp =>
            {
                return new RabbitMQConsumer<T>(
                    rabbitMQPersistentConnection: sp.GetRequiredService<IRabbitMQPersistentConnection>(),
                    service: service,
                    environment: environment,
                    consumerName: consumerName,
                    logger: sp.GetRequiredService<ILogger<RabbitMQConsumer<T>>>());
            });

            return services;
        }

        public static IServiceCollection AddRMQProducer(this IServiceCollection services, string environment, string service, string name)
        {
            services.AddSingleton<IRabbitMQProducer>(sp =>
            {
                return new RabbitMQProducer(
                    rabbitMQPersistentConnection: sp.GetRequiredService<IRabbitMQPersistentConnection>(),
                    logger: sp.GetRequiredService<ILogger<RabbitMQProducer>>(),
                    clock: SystemClock.Instance,
                    service: service,
                    environment: environment,
                    name: name);
            });

            return services;
        }

        public static IHealthChecksBuilder AddDefaultHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var hcBuilder = services.AddHealthChecks();

            // Health check for the application itself
            hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());
            var eventBussConnectionString = configuration.GetValue<string>("EventBusSettings:HostAddress");

            if (!string.IsNullOrEmpty(eventBussConnectionString))
            {
                hcBuilder.AddRabbitMQ(
                    _ => eventBussConnectionString,
                    name: "rabbitmq",
                    tags: new string[] { "ready" });
            }

            return hcBuilder;
        }

        public static void MapDefaultHealthChecks(this IEndpointRouteBuilder routes)
        {
            routes.MapHealthChecks("/hc", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            routes.MapHealthChecks("/liveness", new HealthCheckOptions
            {
                Predicate = r => r.Name.Contains("self")
            });
        }
    }
}

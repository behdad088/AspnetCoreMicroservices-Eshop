using Services.Common;

namespace Basket.API.Extensions
{
    public static class Extensions
    {
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDefaultHealthChecks(configuration);
            services.AddHealthChecks()
                .AddRedis(_ => configuration.GetValue<string>("CacheSettings:ConnectionString"), "redis", tags: new[] { "ready", "liveness" });

            return services;
        }

        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetValue<string>("CacheSettings:ConnectionString");
            });

            return services;
        }
    }
}

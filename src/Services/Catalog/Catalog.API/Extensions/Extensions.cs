using Services.Common;

namespace Catalog.API.Extensions
{
    public static class Extensions
    {
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDefaultHealthChecks(configuration);
            services.AddHealthChecks()
                .AddMongoDb(configuration.GetValue<string>("DatabaseSettings:ConnectionString"), name: "mongodb", tags: new[] { "ready", "liveness" });

            return services;
        }
    }
}

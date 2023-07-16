using Services.Common;

namespace Discount.API.Extensions
{
    public static class Extensions
    {
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDefaultHealthChecks(configuration);
            services.AddHealthChecks()
                .AddNpgSql(configuration.GetValue<string>("DatabaseSettings:ConnectionString"), name: "postgres", tags: new[] { "ready", "liveness" });

            return services;
        }
    }
}

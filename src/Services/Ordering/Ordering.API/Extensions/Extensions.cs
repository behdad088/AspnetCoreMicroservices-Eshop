using Services.Common;

namespace Ordering.API.Extensions
{
    public static class Extensions
    {
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDefaultHealthChecks(configuration);

            var hcBuilder = services.AddHealthChecks();
            var sqlConnectionString = configuration.GetConnectionString("OrderingConnectionString");

            if (!string.IsNullOrEmpty(sqlConnectionString))
            {
                hcBuilder
                    .AddSqlServer(_ => sqlConnectionString, name: "sql-CatalogDB-check", tags: new string[] { "ready" });
            }

            return services;
        }
    }
}

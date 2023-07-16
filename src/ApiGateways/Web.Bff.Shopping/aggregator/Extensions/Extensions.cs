using Services.Common;

namespace Web.Shopping.HttpAggregator.Extensions
{
    internal static class Extensions
    {
        public static IServiceCollection AddReverseProxy(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddReverseProxy().LoadFromConfig(configuration.GetRequiredSection("ReverseProxy"));

            return services;
        }

        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDefaultHealthChecks(configuration);
            services.AddHealthChecks()
                .AddUrlGroup(_ => new Uri(configuration.GetValue<string>("CatalogUrlHC")), name: "catalogapi-check", tags: new string[] { "catalogapi" })
                .AddUrlGroup(_ => new Uri(configuration.GetValue<string>("OrderingUrlHC")), name: "orderingapi-check", tags: new string[] { "orderingapi" })
                .AddUrlGroup(_ => new Uri(configuration.GetValue<string>("BasketUrlHC")), name: "basketapi-check", tags: new string[] { "basketapi" })
                .AddUrlGroup(_ => new Uri(configuration.GetValue<string>("DiscountUrlHC")), name: "discountapi-check", tags: new string[] { "discountapi" });

            return services;
        }
    }
}

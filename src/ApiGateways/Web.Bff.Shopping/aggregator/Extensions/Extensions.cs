namespace Web.Shopping.HttpAggregator.Extensions
{
    internal static class Extensions
    {
        public static IServiceCollection AddReverseProxy(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddReverseProxy().LoadFromConfig(configuration.GetRequiredSection("ReverseProxy"));

            return services;
        }
    }
}

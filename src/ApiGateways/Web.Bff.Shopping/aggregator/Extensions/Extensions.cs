using Basket.Grpc.Protos;
using Catalog.Grpc.Protos;
using Microsoft.Extensions.Options;
using Order.Grpc.Protos;
using Services.Common;
using Web.Shopping.HttpAggregator.Infrastructure;
using Web.Shopping.HttpAggregator.Models.Config;
using Web.Shopping.HttpAggregator.Services;

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

        public static IServiceCollection AddGrpcServices(this IServiceCollection services)
        {
            services.AddTransient<GrpcExceptionInterceptor>();

            services.AddScoped<IBasketService, BasketService>();
            services.AddGrpcClient<BasketProtoService.BasketProtoServiceClient>((services, options) =>
            {
                var basketApi = services.GetRequiredService<IOptions<UrlsConfig>>().Value.GrpcBasket;
                options.Address = new Uri(basketApi);
            }).AddInterceptor<GrpcExceptionInterceptor>();

            services.AddScoped<ICatalogService, CatalogService>();
            services.AddGrpcClient<CatalogProtoService.CatalogProtoServiceClient>((services, options) =>
            {
                var basketApi = services.GetRequiredService<IOptions<UrlsConfig>>().Value.GrpcCatalog;
                options.Address = new Uri(basketApi);
            }).AddInterceptor<GrpcExceptionInterceptor>();

            services.AddScoped<IOrderService, OrderService>();
            services.AddGrpcClient<OrderProtoService.OrderProtoServiceClient>((services, options) =>
            {
                var basketApi = services.GetRequiredService<IOptions<UrlsConfig>>().Value.GrpcOrdering;
                options.Address = new Uri(basketApi);
            }).AddInterceptor<GrpcExceptionInterceptor>();

            return services;
        }
    }
}

using Basket.API.Extensions;
using Basket.API.Grpc;
using Basket.API.Models.Configs;
using Basket.API.Observability;
using Basket.API.Repositories;
using Discount.Grpc.Protos;
using Eshop.BuildingBlocks.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Services.Common;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "eshop.basket.api";
builder.Services.AddOpenTelemetryOtl(serviceName);

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddGrpc();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddHealthChecks(builder.Configuration);
builder.Services.AddScoped<IBasketRepository, BasketRepository>();
builder.Services.Configure<UrlsConfig>(builder.Configuration.GetSection("urls"));
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>((service, option) =>
{
    var discountUrl = service.GetRequiredService<IOptions<UrlsConfig>>().Value.GrpcDiscount;
    option.Address = new Uri(discountUrl);
});

builder.Services.AddRmqConnection(builder.Configuration.GetValue<string>("EventBusSettings:HostAddress")!);
builder.Services.AddRmqProducer("uat", "order", "checkout");

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<TenantIdProcessor>();
builder.Services.ConfigureOpenTelemetryTracerProvider(
        (serviceProvider, traceProviderBuilder) =>
        {
            traceProviderBuilder.AddProcessor(serviceProvider.GetRequiredService<TenantIdProcessor>());
        });

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;
builder.Services.SetupLogging(appName: "Basket.API", environment: environment, elasticSearchConnectionString: builder.Configuration.GetValue<string>("elasticSearchConnectionString"));


var app = builder.Build();
app.MapDefaultHealthChecks();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseAuthorization();
app.MapGrpcService<BasketService>();
app.MapControllers();

await app.RunAsync();

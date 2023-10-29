using Basket.API.Extensions;
using Basket.API.Grpc;
using Basket.API.Models.Configs;
using Basket.API.Repositories;
using Discount.Grpc.Protos;
using Eshop.BuildingBlocks.Logging;
using Microsoft.Extensions.Options;
using Services.Common;

var builder = WebApplication.CreateBuilder(args);
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

builder.Services.AddRMQConnection(builder.Configuration.GetValue<string>("EventBusSettings:HostAddress")!);
builder.Services.AddRMQProducer("uat", "order", "checkout");

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

app.UseAuthorization();
app.MapGrpcService<BasketService>();
app.MapControllers();

await app.RunAsync();

using Basket.API.Models.Configs;
using Basket.API.Repositories;
using Discount.Grpc.Protos;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetValue<string>("CacheSettings:ConnectionString");
});

builder.Services.AddScoped<IBasketRepository, BasketRepository>();
builder.Services.Configure<UrlsConfig>(builder.Configuration.GetSection("urls"));
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>((service, option) =>
{
    var discountUrl = service.GetRequiredService<IOptions<UrlsConfig>>().Value.GrpcDiscount;
    option.Address = new Uri(discountUrl);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

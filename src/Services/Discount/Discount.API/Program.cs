using Discount.API.Extensions;
using Discount.API.Grpc;
using Discount.API.Repositories;
using Eshop.BuildingBlocks.Logging;
using Services.Common;

var builder = WebApplication.CreateBuilder(args);
const string serviceName = "eshop.discount.api";
builder.Services.AddOpenTelemetryOtl(serviceName);

builder.Services.MigrateDatabase<Program>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddGrpc();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks(builder.Configuration);

var postgresConnectionString = builder.Configuration.GetValue<string>("DatabaseSettings:ConnectionString");
builder.Services.AddScoped<IDiscountRepository>(sp =>
{
    return new DiscountRepository(postgresConnectionString);
});

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;
builder.Services.SetupLogging(appName: "Discount.API", environment: environment, elasticSearchConnectionString: builder.Configuration.GetValue<string>("elasticSearchConnectionString"));


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

app.MapGrpcService<DiscountService>();
app.MapControllers();

await app.RunAsync();

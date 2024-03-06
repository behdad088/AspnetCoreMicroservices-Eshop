using Eshop.BuildingBlocks.Logging;
using Services.Common;
using Web.Shopping.HttpAggregator.Extensions;
using Web.Shopping.HttpAggregator.Models.Config;

var builder = WebApplication.CreateBuilder(args);
const string serviceName = "eshop.shopping.httpaggregator.api";
builder.Services.AddOpenTelemetryOtl(serviceName);

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// Add services to the container.
builder.Services.AddReverseProxy(builder.Configuration);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks(builder.Configuration);
builder.Services.AddGrpcServices();
builder.Services.Configure<UrlsConfig>(builder.Configuration.GetSection("urls"));

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;
builder.Services.SetupLogging(appName: "Web.Shopping.HttpAggregator", environment: environment, elasticSearchConnectionString: builder.Configuration.GetValue<string>("elasticSearchConnectionString"));


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

app.MapControllers();
app.MapReverseProxy();

await app.RunAsync();

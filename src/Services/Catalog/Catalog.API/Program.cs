using Catalog.API.Data;
using Catalog.API.Extensions;
using Catalog.API.Grpc;
using Catalog.API.Repositories;
using Microsoft.OpenApi.Models;
using Services.Common;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddGrpc();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog.API", Version = "v1" });
});

builder.Services.AddHealthChecks(builder.Configuration);
builder.Services.AddSingleton<ICatalogContext, CatalogContext>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();
app.MapDefaultHealthChecks();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapGrpcService<CatalogService>();
app.MapControllers();

await app.RunAsync();

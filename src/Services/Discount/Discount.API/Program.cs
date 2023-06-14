using Discount.API.Extensions;
using Discount.API.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.MigrateDatabase<Program>();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnectionString = builder.Configuration.GetValue<string>("DatabaseSettings:ConnectionString");
builder.Services.AddScoped<IDiscountRepository>(sp =>
{
    return new DiscountRepository(postgresConnectionString);
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

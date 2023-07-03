using Microsoft.Extensions.Logging;
using Ordering.Domain.Entities;

namespace Ordering.Infrastructure.Persistence
{
    public class OrderContextSeed
    {
        public static async Task SeedAsync(OrderContext orderContext, ILogger<OrderContextSeed> logger)
        {
            if (!orderContext.Orders.Any())
            {
                orderContext.Orders.AddRange(GetPreconfiguredOrders());
                await orderContext.SaveChangesAsync();
                logger.LogInformation("Seed database associated with context {DbContextName}", typeof(OrderContext).Name);
            }
        }

        private static IEnumerable<Order> GetPreconfiguredOrders()
        {
            return new List<Order>
            {
                new Order() {
                    Username = "behdad",
                    FirstName = "behdad",
                    LastName = "kardgar",
                    EmailAddress = "behdad@gmail.com",
                    AddressLine = "Gothenburg",
                    Country = "Sweden",
                    CVV = "test",
                    CardName = "Test",
                    CardNumber = "Test",
                    CreatedBy = "Test",
                    Expiration = "test",
                    PaymentMethod = 1,
                    State = "test",
                    ZipCode = "test",
                    LastModifiedBy = "Test",
                    TotalPrice = 350
                }
            };
        }
    }
}

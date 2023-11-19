using Npgsql;
using Polly;
using System.Net.Sockets;

namespace Discount.API.Extensions
{
    public static class WebHostExtensions
    {
        public static IServiceCollection MigrateDatabase<TContext>(this IServiceCollection serviceCollection)
        {
            int retryCount = 7;
            using var scope = serviceCollection.BuildServiceProvider().CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();

            var policy = Policy.Handle<SocketException>()
                   .Or<NpgsqlException>()
                   .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                   {
                       logger.LogError(ex, "An error occurred while migrating the postresql database, failed trying after {TimeOut}s", $"{time.TotalSeconds:n1}");
                   }
               );

            policy.Execute(() =>
            {
                MigratePostgresDatabase(configuration, logger);
            });

            return serviceCollection;
        }

        private static void MigratePostgresDatabase<TContext>(IConfiguration configuration, ILogger<TContext> logger)
        {
            logger.LogInformation("Migrating postresql database.");

            using var connection = new NpgsqlConnection
                (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
            connection.Open();

            using var command = new NpgsqlCommand
            {
                Connection = connection
            };

            command.CommandText = "DROP TABLE IF EXISTS Coupon";
            command.ExecuteNonQuery();

            command.CommandText = @"CREATE TABLE Coupon(Id SERIAL PRIMARY KEY, 
                                                                ProductName VARCHAR(24) NOT NULL,
                                                                Description TEXT,
                                                                Amount INT)";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Coupon(ProductName, Description, Amount) VALUES('IPhone X', 'IPhone Discount', 150);";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Coupon(ProductName, Description, Amount) VALUES('Samsung 10', 'Samsung Discount', 100);";
            command.ExecuteNonQuery();

            logger.LogInformation("Migrated postresql database.");
        }
    }
}

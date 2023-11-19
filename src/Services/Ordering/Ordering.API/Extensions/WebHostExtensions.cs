using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Net.Sockets;

namespace Ordering.API.Extensions
{
    public static class WebHostExtensions
    {
        public static IServiceCollection MigrateDatabase<TContext>(this IServiceCollection serviceCollection,
                                            Action<TContext, IServiceProvider> seeder) where TContext : DbContext
        {
            int retryCount = 7;
            using var scope = serviceCollection.BuildServiceProvider().CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
            var context = scope.ServiceProvider.GetService<TContext>();

            var policy = Policy.Handle<SocketException>()
                   .Or<SqlException>()
                   .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                   {
                       logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);
                   }
               );

            policy.Execute(() =>
            {
                logger.LogInformation("Migrating database associated with context {DbContextName} started.", typeof(TContext).Name);
                InvokeSeeder(seeder, context, scope.ServiceProvider);
                logger.LogInformation("Migrated database associated with context {DbContextName} finished.", typeof(TContext).Name);
            });

            return serviceCollection;
        }

        private static void InvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder,
                                                    TContext context,
                                                    IServiceProvider services)
                                                    where TContext : DbContext
        {
            context.Database.Migrate();
            seeder(context, services);
        }
    }
}

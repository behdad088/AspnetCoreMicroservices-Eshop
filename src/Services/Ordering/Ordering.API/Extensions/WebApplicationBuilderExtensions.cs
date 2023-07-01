using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ordering.API.Extensions
{
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder MigrateDatabase<TContext>(this WebApplicationBuilder webApplicationBuilder,
                                            Action<TContext, IServiceProvider> seeder,
                                            int? retry = 0) where TContext : DbContext
        {
            int retryForAvailability = retry.Value;
            using var provider = webApplicationBuilder.Services.BuildServiceProvider();
            using (var scope = provider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<TContext>>();
                var context = services.GetService<TContext>();

                try
                {
                    logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

                    InvokeSeeder(seeder, context, services);

                    logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);
                }
                catch (SqlException ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);

                    if (retryForAvailability < 50)
                    {
                        retryForAvailability++;
                        Thread.Sleep(2000);
                        MigrateDatabase(webApplicationBuilder, seeder, retryForAvailability);
                    }
                }
            }
            return webApplicationBuilder;
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

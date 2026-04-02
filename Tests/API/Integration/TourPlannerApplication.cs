using Autofac;
using DAL.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Tests.API.Integration;

public sealed class TourPlannerApplication(
    Action<IServiceCollection>? configureServices = null,
    Action<ContainerBuilder>? configureContainer = null
)
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString = BuildConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TourPlannerDatabase"] = _connectionString,
                ["AppSettings:OpenRouteServiceApiKey"] = "test-api-key",
                ["AppSettings:OpenRouteServiceApiBaseUrl"] = "https://example.test"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TourPlannerContext>();
            services.RemoveAll<DbContextOptions<TourPlannerContext>>();
            services.AddDbContext<TourPlannerContext>(options =>
                options.UseNpgsql(_connectionString));
            configureServices?.Invoke(services);
        });
        builder.ConfigureTestContainer<ContainerBuilder>(container =>
        {
            container.Register(_ =>
            {
                var dbOptions = new DbContextOptionsBuilder<TourPlannerContext>()
                    .UseNpgsql(_connectionString)
                    .Options;
                return new TourPlannerContext(dbOptions);
            }).AsSelf().InstancePerLifetimeScope();

            configureContainer?.Invoke(container);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TourPlannerContext>();
        await dbContext.Database.MigrateAsync();
    }

    private static string BuildConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(PostgreSqlContainerHost.ConnectionString)
        {
            Database = $"tourplanner_test_{Guid.NewGuid():N}"
        };

        return builder.ConnectionString;
    }
}

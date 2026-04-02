using DAL.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace Tests.API.Integration;

[SetUpFixture]
public sealed class PostgreSqlContainerFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await PostgreSqlContainerHost.EnsureStartedAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await PostgreSqlContainerHost.DisposeAsync();
    }
}

internal static class PostgreSqlContainerHost
{
    private static readonly SemaphoreSlim Sync = new(1, 1);
    private static PostgreSqlContainer? _container;

    public static string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL test container has not been started.");

    public static async Task EnsureStartedAsync()
    {
        if (_container is not null)
        {
            return;
        }

        await Sync.WaitAsync();
        try
        {
            if (_container is not null)
            {
                return;
            }

            var image = Environment.GetEnvironmentVariable("POSTGRES_IMAGE") ?? "postgres:16-alpine";
            _container = new PostgreSqlBuilder(image)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilMessageIsLogged("database system is ready to accept connections"))
                .Build();

            await _container.StartAsync();
        }
        finally
        {
            Sync.Release();
        }
    }

    public static async Task DisposeAsync()
    {
        if (_container is null)
        {
            return;
        }

        await _container.DisposeAsync();
        _container = null;
    }

    public static async Task ResetDatabaseAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TourPlannerContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }
}

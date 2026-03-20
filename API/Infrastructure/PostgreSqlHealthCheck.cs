using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace API.Infrastructure;

public sealed class PostgreSqlHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var connectionString = configuration.GetConnectionString("TourPlannerDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy(
                "Connection string 'TourPlannerDatabase' is missing."
            );
        }

        try
        {
            // Health probes must stay bounded even when PostgreSQL is unreachable.
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Timeout = 1,
                CommandTimeout = 1,
                CancellationTimeout = 1,
            };

            await using var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL connectivity check failed.",
                exception
            );
        }
    }
}

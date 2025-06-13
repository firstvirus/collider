using UserAnalyticsAPI.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace UserAnalyticsAPI.Application.Extensions.Services;

public static class DatabaseConfiguration
{
    public static IServiceCollection ConfigureDatabase<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        string server = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? "PostgreSQL";
        string host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        string port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        string username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgre";
        string password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgre";
        string database = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgre";

        string connectionString = $"Host={host};Port={port};Username={username};Password={password};Database={database}";
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception("DB_CONNECTION environment variable is missing");
        }

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        return services;
    }
}

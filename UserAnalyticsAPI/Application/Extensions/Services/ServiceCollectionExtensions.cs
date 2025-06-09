using UserAnalyticsAPI.Domain.Data;

namespace UserAnalyticsAPI.Application.Extensions.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureDatabase<MainDbContext>(configuration);
        services.ConfigureSwagger();
        services.ConfigureCors(configuration);

        services.ConfigureCustomServices();

        return services;
    }
}

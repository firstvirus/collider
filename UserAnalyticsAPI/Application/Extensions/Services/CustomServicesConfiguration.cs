using UserAnalyticsAPI.Domain.UseCases;

namespace UserAnalyticsAPI.Application.Extensions.Services;

public static class CustomServicesConfiguration
{
    public static IServiceCollection ConfigureCustomServices(this IServiceCollection services)
    {
        services.AddTransient<AddEventUseCase>();
        services.AddTransient<ListPagedEventsUseCase>();
        services.AddTransient<StatisticUseCase>();

        return services;
    }
}

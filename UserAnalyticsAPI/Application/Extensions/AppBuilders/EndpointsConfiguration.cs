namespace UserAnalyticsAPI.Application.Extensions.AppBuilders;

public static class EndpointsConfiguration
{
    public static IApplicationBuilder ConfigureEndpoints(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers(); // Явно задаем маппинг контроллеров
        });
        return app;
    }
}
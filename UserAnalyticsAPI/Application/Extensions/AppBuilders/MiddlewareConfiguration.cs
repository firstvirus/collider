namespace UserAnalyticsAPI.Application.Extensions.AppBuilders;

public static class MiddlewareConfiguration
{
    public static IApplicationBuilder ConfigureMiddleware(this IApplicationBuilder app)
    {
        app.UseCors("CorsPolicy");
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
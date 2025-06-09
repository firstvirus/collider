namespace UserAnalyticsAPI.Application.Extensions.AppBuilders;

public static class SwaggerUiConfiguration
{
    public static IApplicationBuilder UseSwaggerWithUi(this IApplicationBuilder app)
    {
        app.UseSwagger(); // Генерация Swagger документации
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Service API v1");
            options.RoutePrefix = string.Empty; // Делает Swagger доступным по корню приложения
        });

        return app;
    }
}
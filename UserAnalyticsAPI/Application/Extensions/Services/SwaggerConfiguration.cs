using UserAnalyticsAPI.Application.Extensions.Filters;

namespace UserAnalyticsAPI.Application.Extensions.Services;

public static class SwaggerConfiguration
{
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add(new ValidateModelAttribute());
        });

        services.AddEndpointsApiExplorer(); // Для минимальных API
        services.AddSwaggerGen(options =>
        {
            // Получаем значения из переменных окружения с fallback-значениями
            var swaggerVersion = Environment.GetEnvironmentVariable("SWAGGER_VERSION") ?? "v1";
            var swaggerTitle = Environment.GetEnvironmentVariable("SWAGGER_TITLE") ?? "NONAME Service API";
            var swaggerDescription = Environment.GetEnvironmentVariable("SWAGGER_DESCRIPTION") ?? """
                ## ENV не подгружены!!!
                ### Общая информация:
                1. Методы требующие Bearier авторизации помечены 🔒
                2. Используется JWE токен
                """;

            options.SwaggerDoc(swaggerVersion, new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Version = swaggerVersion,
                Title = swaggerTitle,
                Description = swaggerDescription
            });

            // Если используете JWT авторизацию
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Введите JWE access токен"
            });

            options.EnableAnnotations();
        });

        return services;
    }
}

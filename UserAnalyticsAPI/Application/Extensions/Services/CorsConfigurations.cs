namespace UserAnalyticsAPI.Application.Extensions.Services;

public static class CorsConfigurations
{
    public static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = Environment.GetEnvironmentVariable("CORS_ORIGINS") 
                      ?? configuration["CORS_ORIGINS"];

        var allowedOrigins = origins?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) 
                             ?? new string[0];

        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
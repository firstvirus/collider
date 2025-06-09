using DotNetEnv;
using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using UserAnalyticsAPI.Application.Extensions.AppBuilders;
using UserAnalyticsAPI.Application.Extensions.Services;
using UserAnalyticsAPI.Domain.Data;
using UserAnalyticsAPI.Domain.Data.Seeders;

//Env.Load("../config.env");

// ������� �������� �������
var rootCommand = new RootCommand("User Analytics API");
var seedCommand = new Command("seed", "Seed database with test data");
rootCommand.AddCommand(seedCommand);

// ������ ��������� ��������� ������
if (args.Contains("seed"))
{
    // ������ ����������� ��� ������� ����������
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    string server = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? "PostgreSQL";
    string host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    string port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    string username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgre";
    string password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgre";
    string database = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgre";

    string connectionString = $"Host={host};Port={port};Username={username};Password={password};Database={database};Command Timeout=300;Timeout=300;";
    Console.WriteLine(connectionString);

    builder.Services.AddDbContext<MainDbContext>(options =>
                    options.UseNpgsql(connectionString)
                        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

    builder.Services.AddScoped<DbSeeder>();

    using WebApplication app = builder.Build();
    using IServiceScope scope = app.Services.CreateScope();
    MainDbContext dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    await dbContext.Database.MigrateAsync();
    DbSeeder seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
    
    Console.WriteLine("Seeding completed successfully");
    Environment.Exit(0); 
}


WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);

webAppBuilder.Configuration.AddEnvironmentVariables();

webAppBuilder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };
    });
webAppBuilder.Services.ConfigureServices(webAppBuilder.Configuration);


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
webAppBuilder.Services.AddEndpointsApiExplorer();
webAppBuilder.Services.AddSwaggerGen();

WebApplication webApp = webAppBuilder.Build();

webApp.ConfigureMiddleware();
webApp.ConfigureEndpoints();
webApp.UseSwaggerWithUi();
webApp.Run();

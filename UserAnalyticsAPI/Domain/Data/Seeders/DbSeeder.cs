using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using System.Diagnostics;
using UserAnalyticsAPI.Domain.Data.Models;

namespace UserAnalyticsAPI.Domain.Data.Seeders;

public class DbSeeder(MainDbContext mainDbContext)
{
    private static readonly Random Random = new();
    private static readonly string[] EventNames = ["login", "logout", "cart", "view", "search", "checkout"];
    private static readonly string[] FirstNames = ["John", "Alice", "Bob", "Emma", "Michael", "Izya"];
    private static readonly string[] LastNames = ["Smith", "Johnson", "Brown", "Davis", "Wilson"];

    public async Task SeedAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        await SeedUsers(mainDbContext);
        await SeedEventTypes(mainDbContext);
        await SeedEvents(mainDbContext);
        stopwatch.Stop();
        Console.WriteLine($"Execution time is: {stopwatch.ElapsedMilliseconds} ms");
    }

    private static async Task SeedUsers(MainDbContext context)
    {
        if (await context.Users.AnyAsync()) return;

        List<User> users = new();
        for (int i = 0; i < 100; i++)
        {
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                Name = $"{FirstNames[Random.Next(FirstNames.Length)]} {LastNames[Random.Next(LastNames.Length)]}",
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Next(1, 365))
            });
        }

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }

    private static async Task SeedEventTypes(MainDbContext context)
    {
        if (await context.EventTypes.AnyAsync()) return;

        var eventTypes = EventNames.Select((name, index) => new EventType
        {
            Id = index + 1,
            Name = name
        });

        await context.EventTypes.AddRangeAsync(eventTypes);
        await context.SaveChangesAsync();
    }

    private static async Task SeedEvents(MainDbContext context)
    {
        if (await context.Events.AnyAsync()) return;

        var users = await context.Users.ToListAsync();
        var eventTypes = await context.EventTypes.ToListAsync();
        var events = new List<Event>();

        await context.Database.ExecuteSqlRawAsync("ALTER TABLE events SET UNLOGGED");
        await context.Database.ExecuteSqlRawAsync("DROP INDEX ix_events_user_id");
        
        var npgsqlConnection = (NpgsqlConnection)context.Database.GetDbConnection();

        /*if (npgsqlConnection.State != System.Data.ConnectionState.Open)
            await npgsqlConnection.OpenAsync();


        await using var writer = await npgsqlConnection.BeginBinaryImportAsync(
            "COPY events (user_id, type_id, timestamp, metadata) FROM STDIN (FORMAT BINARY)");

        for (int i = 0; i < 10000000; i++)
        {
            var user = users[Random.Next(users.Count)];
            var eventType = eventTypes[Random.Next(eventTypes.Count)];

            await writer.StartRowAsync();
            await writer.WriteAsync(user.Id);
            await writer.WriteAsync(eventType.Id);
            await writer.WriteAsync(DateTime.UtcNow.AddMinutes(-Random.Next(1, 10080)));
            await writer.WriteAsync(JsonConvert.SerializeObject(GenerateRandomMetadata(eventType.Name)), NpgsqlDbType.Jsonb);
        }

        await writer.CompleteAsync();
        await writer.DisposeAsync();*/

        for (int i = 0; i < 200; i++)
        {
            string query = "INSERT INTO events (user_id,type_id,timestamp,metadata) VALUES ";
            for (int j = 0; j < 50000; j++)
            {
                var user = users[Random.Next(users.Count)];
                var eventType = eventTypes[Random.Next(eventTypes.Count)];
                query += (j == 0) ? "" : ",";
                query += $"({user.Id},{eventType.Id},{DateTime.UtcNow.AddMinutes(-Random.Next(1, 10080))},{JsonConvert.SerializeObject(GenerateRandomMetadata(eventType.Name))})";
            }
            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);
            await cmd.ExecuteNonQueryAsync();
        }

        await context.Database.ExecuteSqlRawAsync("CREATE INDEX CONCURRENTLY ix_events_user_id ON events(user_id)");
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE events SET LOGGED");
    }

    private static Dictionary<string, object> GenerateRandomMetadata(string eventType)
    {
        var metadata = new Dictionary<string, object>
        {
            ["ip"] = $"{Random.Next(1, 255)}.{Random.Next(0, 255)}.{Random.Next(0, 255)}.{Random.Next(0, 255)}",
            ["userAgent"] = Random.NextDouble() > 0.5 ? "Chrome" : "Firefox",
            ["page"] = $"/{eventType}"
        };

        switch (eventType)
        {
            case "checkout":
                metadata["amount"] = Math.Round(Random.NextDouble() * 100, 2);
                metadata["items"] = Random.Next(1, 5);
                break;
            case "search":
                metadata["query"] = $"search query {Random.Next(1000)}";
                break;
        }

        return metadata;
    }
}

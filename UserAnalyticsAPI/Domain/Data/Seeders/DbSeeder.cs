using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Concurrent;
using System.Data;
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

        // 1. Подготовка БД (максимальная производительность)
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE events SET UNLOGGED;
            DROP INDEX IF EXISTS ix_events_user_id;
            ALTER TABLE events DISABLE TRIGGER ALL;
        ");

        // 2. Загрузка только необходимых данных
        var userIds = await context.Users.AsNoTracking()
            .Select(u => u.Id)
            .ToArrayAsync();

        var eventTypes = await context.EventTypes.AsNoTracking()
            .Select(et => new { et.Id, et.Name })
            .ToArrayAsync();

        // 3. Параллельное заполнение (разбивка на потоки)
        const int totalRecords = 10_000_000;
        var batchSize = 250_000;
        var random = new Random();
        int maxThreads = Environment.ProcessorCount;
        Console.WriteLine($"Number of cores: {maxThreads}");

        await Parallel.ForEachAsync(
            Partitioner.Create(0, totalRecords, batchSize).GetDynamicPartitions(), // Без batchSize!
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreads, // Жестко фиксируем потоки
                TaskScheduler = TaskScheduler.Default
            },
            async (range, ct) =>
            {
                Console.WriteLine($"{range.Item1} - {range.Item2} started.");
                var npgsqlConnection = new NpgsqlConnection(context.Database.GetConnectionString());

                if (npgsqlConnection.State != System.Data.ConnectionState.Open)
                    await npgsqlConnection.OpenAsync();

                await using var writer = await npgsqlConnection.BeginBinaryImportAsync(
                    "COPY events (user_id, type_id, timestamp, metadata) FROM STDIN (FORMAT BINARY)");

                var events = new Event[range.Item2 - range.Item1];
                var random = new Random(Environment.TickCount + range.Item1);

                for (int i = 0; i < events.Length; i++)
                {
                    var userIdx = random.Next(userIds.Length);
                    var typeIdx = random.Next(eventTypes.Length);
                    var eventType = eventTypes[typeIdx];

                    await writer.StartRowAsync();
                    await writer.WriteAsync(userIds[userIdx]);
                    await writer.WriteAsync(eventType.Id);
                    await writer.WriteAsync(DateTime.UtcNow.AddMinutes(-Random.Next(1, 10080)));
                    await writer.WriteAsync(JsonConvert.SerializeObject(GenerateRandomMetadata(eventType.Name)), NpgsqlDbType.Jsonb);
                }

                await writer.CompleteAsync();
                await writer.DisposeAsync();
                await npgsqlConnection.CloseAsync();
                await npgsqlConnection.DisposeAsync();
                Console.WriteLine($"{range.Item1} - {range.Item2} finished.");
            }
        );

        // 4. Восстановление БД
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_events_user_id ON events(user_id)");
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE events ENABLE TRIGGER ALL");
    }

    private static Event GenerateEvent(Guid userId, dynamic eventType, Random random)
    {
        return new Event
        {
            UserId = userId,
            TypeId = eventType.Id,
            Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(1, 10080)),
            Metadata = new Dictionary<string, object> { ["path"] = "/" + eventType.Name }
        };
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

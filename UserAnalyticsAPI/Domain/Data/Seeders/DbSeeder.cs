using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Newtonsoft.Json;
using Npgsql;
using Npgsql.Bulk;
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
            TRUNCATE TABLE events;
            ALTER TABLE events DISABLE TRIGGER ALL;
        ");

        // 2. Загрузка только необходимых данных
        var userIds = await context.Users.AsNoTracking()
            .Select(u => u.Id)
            .ToArrayAsync();

        var eventTypes = await context.EventTypes.AsNoTracking()
            .Select(et => new { et.Id, et.Name })
            .ToArrayAsync();

        // 3. Настройка подключения
        var connectionString = context.Database.GetConnectionString();

        // 4. Параллельное заполнение DataTable (разбивка на потоки)
        const int totalRecords = 10_000_000;
        const int batchSize = 100_000;
        var random = new Random();

        await Parallel.ForEachAsync(
            Partitioner.Create(0, totalRecords, batchSize).GetDynamicPartitions(),
            async (range, ct) =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<MainDbContext>();
                optionsBuilder.UseNpgsql(connectionString);
                await using var parallelContext = new MainDbContext(optionsBuilder.Options);
                var transaction = await parallelContext.Database.BeginTransactionAsync(ct);

                var events = new Event[range.Item2 - range.Item1];
                var random = new Random(Environment.TickCount + range.Item1);

                for (int i = 0; i < events.Length; i++)
                {
                    var userIdx = random.Next(userIds.Length);
                    var typeIdx = random.Next(eventTypes.Length);
                    events[i] = GenerateEvent(userIds[userIdx], eventTypes[typeIdx], random);
                }

                // 5. Пакетная вставка через NpgsqlBulkUploader
                lock (typeof(NpgsqlBulkUploader))
                {
                    var bulkImporter = new NpgsqlBulkUploader(context);
                    bulkImporter.Insert(events); // Синхронная версия для thread-safety
                }
                await transaction.CommitAsync(ct);
            }
        );

        // 6. Восстановление БД (параллельно)
        await Task.WhenAll(
            context.Database.ExecuteSqlRawAsync("ALTER TABLE events SET LOGGED"),
            context.Database.ExecuteSqlRawAsync("CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_events_user_id ON events(user_id)"),
            context.Database.ExecuteSqlRawAsync("ALTER TABLE events ENABLE TRIGGER ALL")
        );
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

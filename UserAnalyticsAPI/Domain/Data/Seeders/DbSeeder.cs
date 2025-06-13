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

        /*
        var users = await context.Users.ToListAsync();
        var eventTypes = await context.EventTypes.ToListAsync();
        var events = new List<Event>();

        await context.Database.ExecuteSqlRawAsync("ALTER TABLE events SET UNLOGGED");
        await context.Database.ExecuteSqlRawAsync("DROP INDEX ix_events_user_id");
        
        var npgsqlConnection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (npgsqlConnection.State != System.Data.ConnectionState.Open)
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
        await writer.DisposeAsync();

        await context.Database.ExecuteSqlRawAsync("CREATE INDEX CONCURRENTLY ix_events_user_id ON events(user_id)");
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE events SET LOGGED");
        */
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
        await using var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();

        // 4. Создание DataTable с оптимальной структурой
        var dataTable = new DataTable();
        dataTable.Columns.Add("user_id", typeof(Guid));
        dataTable.Columns.Add("type_id", typeof(int));
        dataTable.Columns.Add("timestamp", typeof(DateTime));
        dataTable.Columns.Add("metadata", typeof(string)); // Будет конвертировано в jsonb

        // 5. Подготовка кеша JSON
        var jsonCache = eventTypes.ToDictionary(
            et => et.Id,
            et => JsonConvert.SerializeObject(new { type = et.Name })
        );

        // 6. Параллельное заполнение DataTable (разбивка на потоки)
        const int totalRecords = 10_000_000;
        const int batchSize = 100_000;
        var random = new Random();

        await Parallel.ForEachAsync(
            Partitioner.Create(0, totalRecords, batchSize).GetDynamicPartitions(),
            async (range, ct) =>
            {
                /*var localTable = new DataTable();
                localTable.Columns.Add("user_id", typeof(Guid));
                localTable.Columns.Add("type_id", typeof(int));
                localTable.Columns.Add("timestamp", typeof(DateTime));
                localTable.Columns.Add("metadata", typeof(string));

                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var userIdx = random.Next(userIds.Length);
                    var typeIdx = random.Next(eventTypes.Length);
                    var eventType = eventTypes[typeIdx];

                    localTable.Rows.Add(
                        userIds[userIdx],
                        eventType.Id,
                        DateTime.UtcNow.AddMinutes(-random.Next(1, 10080)),
                        jsonCache[eventType.Id]
                    );
                }*/
                List<Event> events = new List<Event>();
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var userIdx = random.Next(userIds.Length);
                    var typeIdx = random.Next(eventTypes.Length);
                    var eventType = eventTypes[typeIdx];

                    events.Add(new Event {
                        UserId = userIds[userIdx],
                        TypeId = eventType.Id,
                        Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(1, 10080)),
                        Metadata = GenerateRandomMetadata(eventType.Name)
                    });
                }

                // 7. Пакетная вставка через NpgsqlBulkUploader
                var bulkImporter = new NpgsqlBulkUploader(context, true);
                await bulkImporter.InsertAsync(events);
            }
        );

        // 8. Восстановление БД (параллельно)
        await Task.WhenAll(
            context.Database.ExecuteSqlRawAsync("ALTER TABLE events SET LOGGED"),
            context.Database.ExecuteSqlRawAsync("CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_events_user_id ON events(user_id)"),
            context.Database.ExecuteSqlRawAsync("ALTER TABLE events ENABLE TRIGGER ALL")
        );
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

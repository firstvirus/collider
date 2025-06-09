using Microsoft.EntityFrameworkCore;
using System;
using UserAnalyticsAPI.Domain.Data.Models;

namespace UserAnalyticsAPI.Domain.Data.Seeders;

public class DbSeeder
{
    private readonly MainDbContext _mainDbContext;
    private static readonly Random _random = new();
    private static readonly string[] _eventNames = { "login", "logout", "cart", "view", "search", "checkout" };
    private static readonly string[] _firstNames = { "John", "Alice", "Bob", "Emma", "Michael", "Izya" };
    private static readonly string[] _lastNames = { "Smith", "Johnson", "Brown", "Davis", "Wilson" };

    public DbSeeder(MainDbContext mainDbContext)
    {
        _mainDbContext = mainDbContext;
    }

    public async Task SeedAsync()
    {
        await SeedUsers(_mainDbContext);
        await SeedEventTypes(_mainDbContext);
        await SeedEvents(_mainDbContext);
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
                Name = $"{_firstNames[_random.Next(_firstNames.Length)]} {_lastNames[_random.Next(_lastNames.Length)]}",
                CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 365))
            });
        }

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }

    private static async Task SeedEventTypes(MainDbContext context)
    {
        if (await context.EventTypes.AnyAsync()) return;

        var eventTypes = _eventNames.Select((name, index) => new EventType
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

        for (int i = 0; i < 10000000; i++)
        {
            var user = users[_random.Next(users.Count)];
            var eventType = eventTypes[_random.Next(eventTypes.Count)];

            events.Add(new Event
            {
                UserId = user.Id,
                TypeId = eventType.Id,
                Timestamp = DateTime.UtcNow.AddMinutes(-_random.Next(1, 10080)), // До 7 дней назад
                Metadata = GenerateRandomMetadata(eventType.Name)
            });
        }

        await context.Events.AddRangeAsync(events);
        await context.SaveChangesAsync();
    }

    private static Dictionary<string, object> GenerateRandomMetadata(string eventType)
    {
        var metadata = new Dictionary<string, object>
        {
            ["ip"] = $"{_random.Next(1, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}",
            ["userAgent"] = _random.NextDouble() > 0.5 ? "Chrome" : "Firefox",
            ["page"] = $"/{eventType}"
        };

        switch (eventType)
        {
            case "checkout":
                metadata["amount"] = Math.Round(_random.NextDouble() * 100, 2);
                metadata["items"] = _random.Next(1, 5);
                break;
            case "search":
                metadata["query"] = $"search query {_random.Next(1000)}";
                break;
        }

        return metadata;
    }
}

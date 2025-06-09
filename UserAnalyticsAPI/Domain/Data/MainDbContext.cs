using Microsoft.EntityFrameworkCore;
using Npgsql;
using Newtonsoft.Json;
using System.Text.Json;
using UserAnalyticsAPI.Domain.Data.Models;

namespace UserAnalyticsAPI.Domain.Data;

public class MainDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<EventType> EventTypes { get; set; }

    public DbSet<Event> Events { get; set; }

    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MainDbContext).Assembly);

        modelBuilder.Entity<Event>(e =>
        {
            e.Property(e => e.Metadata)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Dictionary<string, object>>(v) ?? new Dictionary<string, object>()
            );
        });

        // Индекс по пользователю
        modelBuilder.Entity<Event>()
            .HasIndex(e => e.UserId)
            .HasDatabaseName("ix_events_user_id");

        // Индекс по типу события
        modelBuilder.Entity<Event>()
            .HasIndex(e => e.TypeId)
            .HasDatabaseName("ix_events_type_id");

        // Индекс по времени события
        modelBuilder.Entity<Event>()
            .HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_events_timestamp");
    }

    private bool IsPostgres()
    {
        return Database.GetDbConnection() is NpgsqlConnection;
    }
}

using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserAnalyticsAPI.Domain.Data.Models;

/// <summary>
/// События системы
/// </summary>
[Table("events")]
public class Event
{
    /// <summary>
    /// Уникальный идентификатор события (PK)
    /// </summary>
    [Column("id")]
    [Key]
    [Comment("Первичный ключ события")]
    public int Id { get; set; }

    /// <summary>
    /// Ссылка на пользователя (FK)
    /// </summary>
    [Column("user_id")]
    [Required]
    [ForeignKey("user_id")]
    [Comment("Внешний ключ на таблицу users")]
    public required Guid UserId { get; set; }

    /// <summary>
    /// Ссылка на тип события (FK)
    /// </summary>
    [Column("type_id")]
    [Required]
    [ForeignKey("type_id")]
    [Comment("Внешний ключ на таблицу event_types")]
    public required int TypeId { get; set; }

    /// <summary>
    /// Временная метка события
    /// </summary>
    [Column("timestamp")]
    [Required]
    [Comment("Дата и время возникновения события")]
    public required DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дополнительные метаданные события (JSON)
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    [Comment("Дополнительные данные события в формате JSON")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Пользователь (навигационное свойство)
    /// </summary>
    [NotMapped]
    [ForeignKey("user_id")]
    public User? User { get; set; }

    /// <summary>
    /// Тип события (навигационное свойство)
    /// </summary>
    [NotMapped]
    [ForeignKey("type_id")]
    public EventType? EventType { get; set; }
}
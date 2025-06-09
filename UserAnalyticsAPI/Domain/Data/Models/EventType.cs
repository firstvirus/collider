using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserAnalyticsAPI.Domain.Data.Models;

/// <summary>
/// Типы событий системы
/// </summary>
[Table("event_types")]
public class EventType
{
    /// <summary>
    /// Уникальный идентификатор типа события (PK)
    /// </summary>
    [Column("id")]
    [Key]
    [Comment("Первичный ключ типа события")]
    public int Id { get; set; }

    /// <summary>
    /// Название типа события
    /// </summary>
    [Column("name")]
    [Required]
    [MaxLength(100)]
    [Comment("Наименование типа события")]
    public required string Name { get; set; }

    /// <summary>
    /// События этого типа (навигационное свойство)
    /// </summary>
    [NotMapped]
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

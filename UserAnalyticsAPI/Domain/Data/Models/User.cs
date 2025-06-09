using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserAnalyticsAPI.Domain.Data.Models;

/// <summary>
/// Пользователи системы
/// </summary>
[Table("users")]
public class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя (PK)
    /// </summary>
    [Column("id")]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Comment("Первичный ключ пользователя")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Имя пользователя
    /// </summary>
    [Column("name")]
    [Required]
    [MaxLength(255)]
    [Comment("Полное имя пользователя")]
    public required string Name { get; set; }

    /// <summary>
    /// Дата создания пользователя
    /// </summary>
    [Column("created_at")]
    [Required]
    [Comment("Дата и время регистрации пользователя")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// События пользователя (навигационное свойство)
    /// </summary>
    [NotMapped]
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

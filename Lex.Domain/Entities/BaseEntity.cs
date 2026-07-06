using System.ComponentModel.DataAnnotations;

namespace Lex.Domain.Entities;

public abstract class BaseEntity
{
    [Key] public Guid Id { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}
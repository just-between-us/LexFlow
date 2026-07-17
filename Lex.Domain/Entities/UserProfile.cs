using System.ComponentModel.DataAnnotations;

namespace Lex.Domain.Entities;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(200)]
    public string? JobTitle { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(200)]
    public string? Region { get; set; }

    public DateTime? BirthDate { get; set; }
}
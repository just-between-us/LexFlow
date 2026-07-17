using System.ComponentModel.DataAnnotations;
using Lex.Domain.Enums;

namespace Lex.Domain.DTOs;

public class ProfileEditModel
{
    [Required(ErrorMessage = "Введите имя")]
    [MaxLength(100, ErrorMessage = "Максимум 100 символов")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Введите фамилию")]
    [MaxLength(100, ErrorMessage = "Максимум 100 символов")]
    public string? LastName { get; set; }

    public UserActivityType ActivityType { get; set; }

    [MaxLength(200, ErrorMessage = "Максимум 200 символов")]
    public string? JobTitle { get; set; }

    [MaxLength(200, ErrorMessage = "Максимум 200 символов")]
    public string? CompanyName { get; set; }

    [MaxLength(200, ErrorMessage = "Максимум 200 символов")]
    public string? Region { get; set; }

    public DateTime? BirthDate { get; set; }

    // Только для отображения в шапке — не редактируются в этой форме
    public string? Email { get; init; }
    public DateTime MemberSinceUtc { get; init; }
    public bool IsActive { get; init; }
    public string? OrganizationName { get; init; }
}
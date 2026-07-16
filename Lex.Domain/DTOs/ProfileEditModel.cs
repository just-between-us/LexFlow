using Lex.Domain.Enums;

namespace Lex.Domain.DTOs;

public class ProfileEditModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public UserActivityType ActivityType { get; set; }
    public string? JobTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? Region { get; set; }
    public DateOnly? BirthDate { get; set; }
}
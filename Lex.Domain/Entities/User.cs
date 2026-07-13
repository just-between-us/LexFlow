using Lex.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Lex.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public UserActivityType ActivityType { get; set; } = UserActivityType.Guest;

    // Связь с организацией (Один-ко-многим: в одной организации много пользователей)
    public Guid? ClientOrganizationId { get; set; }
    public ClientOrganization? ClientOrganization { get; set; }
    
    public UserProfile? Profile { get; set; }
    
    public ICollection<ActiveChecklist> EditableActiveChecklists  { get; set; } = new List<ActiveChecklist>();// Чек листы, где этот юзер является создателем
    
    public ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();// Документы, где этот юзер является создателем
    
    public ICollection<Document> EditableDocuments { get; set; } = new List<Document>();// Документы, к которым у юзера есть доступ на редактирование (Многие-ко-многим)
    
    public ICollection<ActiveChecklist> ActiveChecklists { get; set; } = new List<ActiveChecklist>();// Чек-листы пользователя
    public string? GetFullName() => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}


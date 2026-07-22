namespace Lex.Domain.Entities;

public class ActiveChecklist : BaseEntity 
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid ChecklistId { get; set; } // Оригинальный шаблон чек-листа
    public Checklist Checklist { get; set; } = null!;
    
    public Guid? ClientOrganizationId { get; set; } // К какой компании привязан
    public ClientOrganization? ClientOrganization { get; set; }
    
    public ICollection<User> Editors { get; set; } = new List<User>();// Редакторы (Многие-ко-многим). 
    
    public ICollection<ActiveChecklistItem> Items { get; set; } = new List<ActiveChecklistItem>();// НАВИГАЦИОННОЕ СВОЙСТВО: Индивидуальное состояние шагов этого чек-листа
}
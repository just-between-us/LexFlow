namespace Lex.Domain.Entities;

public class ChecklistItem : BaseEntity
{
    public string Title { get; set; } = String.Empty;
    public string Description { get; set; } = String.Empty;
    public string Content { get; set; } = String.Empty;
    
    public DateTime? EditedAt { get; set; } = null!; // фактически бесполезно
    public bool IsCompleted { get; set; } = false; // фактически бесполезно
    public int Order { get; set; }
    public Guid ChecklistId { get; set; }// Обратная связь (внешний ключ на родительский чек-лист)
    public Checklist Checklist { get; set; } = null!;
}
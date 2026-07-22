namespace Lex.Domain.Entities;

public class ActiveChecklistItem : BaseEntity
{
    public Guid ActiveChecklistId { get; set; }
    public ActiveChecklist ActiveChecklist { get; set; } = null!;

    public string Title { get; set; } = string.Empty; // Скопировано из ChecklistItem для сохранения истории
    public int Order { get; set; }

    
    public bool IsCompleted { get; set; }// ТЕКУЩЕЕ СОСТОЯНИЕ (То, что меняет пользователь в интерфейсе)
    public string? Note { get; set; } // Заметки к конкретному шагу
}
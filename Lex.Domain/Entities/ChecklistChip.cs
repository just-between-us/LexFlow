namespace Lex.Domain.Entities;

public class ChecklistChip
{
    public Guid Id { get; set; }
    public Guid ChecklistId { get; set; }
    public string Chip { get; set; } = string.Empty;
    
    public Checklist Checklist { get; set; } = null!;
}
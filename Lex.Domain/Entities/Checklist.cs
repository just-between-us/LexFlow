namespace Lex.Domain.Entities;

public class Checklist : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public ICollection<ChecklistChip> Chips { get; set; } = new List<ChecklistChip>();
    
    public ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
}
namespace Lex.Domain.DTOs;

public class ActiveChecklistDto
{
    public Guid Id { get; set; }
    public Guid ChecklistId { get; set; }
    public string ChecklistTitle { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
}
public class ActiveChecklistDetailsDto
{
    public Guid Id { get; set; }
    public Guid ChecklistId { get; set; }
    public string ChecklistTitle { get; set; } = string.Empty;
    public string ChecklistDescription { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public List<ActiveChecklistItemDto> Items { get; set; } = new();
}

public class ActiveChecklistItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsCompleted { get; set; }
    public string? Note { get; set; }
}
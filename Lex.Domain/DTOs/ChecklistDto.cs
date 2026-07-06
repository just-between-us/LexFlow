namespace Lex.Domain.DTOs;

public class ChecklistDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Chips { get; set; } = new();
    public int ItemsCount { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsPopular { get; set; }
    public bool IsNew { get; set; }
}
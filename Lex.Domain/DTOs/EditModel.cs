namespace Lex.Application.DTOs;

public class EditModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Chips { get; set; } = new();
    public List<ItemModel> Items { get; set; } = new();
}
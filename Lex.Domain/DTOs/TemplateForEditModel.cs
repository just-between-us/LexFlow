using Lex.Domain.Enums;

namespace Lex.Domain.DTOs;

public class TemplateForEditModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CurrentContent { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
}
using Lex.Domain.Enums;

namespace Lex.Domain.DTOs;

public class DocumentEditModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public DocumentPrivacy Privacy { get; set; }
    public string CurrentContent { get; set; } = string.Empty;
}
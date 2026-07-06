using Lex.Domain.Enums;

namespace Lex.Domain.DTOs;

public class DocumentTemplateDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public int HintsCount { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsPopular { get; set; }
    public bool IsNew { get; set; }
    
    public string TypeDisplayName => Type switch
    {
        DocumentType.Contract => "Договор",
        DocumentType.Claim => "Иск",
        DocumentType.Policy => "Политика",
        DocumentType.Agreement => "Соглашение",
        DocumentType.Consent => "Согласие",
        _ => "Прочее"
    };
}
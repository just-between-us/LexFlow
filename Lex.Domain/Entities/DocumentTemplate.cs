using System.ComponentModel.DataAnnotations;
using Lex.Domain.Enums;

namespace Lex.Domain.Entities;

public class DocumentTemplate : BaseEntity
{
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    
    [Required, MaxLength(150000)]
    public string CurrentContent { get; set; } = string.Empty; //.md
    
    public DocumentType Type { get; set; } = DocumentType.Other;
    
    public ICollection<TemplateHint> Hints { get; set; } = new List<TemplateHint>(); //Советы
}
using Lex.Domain.Enums;

namespace Lex.Domain.Entities;

public class TemplateHint : BaseEntity //Советы по шаблону 
{
    public Guid DocumentTemplateId { get; set; }
    public DocumentTemplate DocumentTemplate { get; set; } = null!;

    public string Text { get; set; } = string.Empty;
    public int Order { get; set; } // Порядок отображения на панели в MudBlazor
    public HintImportance Importance { get; set; } = HintImportance.Info; // Info, Warning, Critical
}
using System.ComponentModel.DataAnnotations.Schema;
using Lex.Domain.Entities;

namespace Lex.Domain.Entities;

public class DocumentVersion : BaseEntity
{
    // Привязка версии к конкретному документу (Один-ко-многим)
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!; 

    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty; // Текст документа на момент сохранения версии
    public string? ChangeSummary { get; set; } // Комментарий к изменениям

    // Кто создал эту конкретную версию
    public Guid? VersionCreatedByUserId { get; set; }
    public User? VersionCreatedByUser { get; set; }
}
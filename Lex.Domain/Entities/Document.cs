using Lex.Domain.Enums;

namespace Lex.Domain.Entities;

public class Document : DocumentTemplate
{
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public DocumentPrivacy Privacy { get; set; } = DocumentPrivacy.Private;

    // Привязка к оригинальному шаблону
    public Guid? TemplateId { get; set; }
    public DocumentTemplate? Template { get; set; }

    // Создатель документа (Один-ко-многим)
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    
    // Список редакторов (Многие-ко-многим).
    public ICollection<User> Editors { get; set; } = new List<User>();

    // Привязка к организации (если документ расшарен на компанию)
    public Guid? ClientOrganizationId { get; set; } 
    public ClientOrganization? ClientOrganization { get; set; }

    public int CurrentVersionNumber { get; set; } = 1;

    public DateTime? SignedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}
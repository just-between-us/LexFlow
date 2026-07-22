using System.ComponentModel.DataAnnotations;
using Lex.Domain.Enums;

namespace Lex.Domain.Entities;

public class Document : BaseEntity  
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(150000)]
    public string CurrentContent { get; set; } = string.Empty;

    public DocumentType Type { get; set; } = DocumentType.Other;

    // Специфические поля документа
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public DocumentPrivacy Privacy { get; set; } = DocumentPrivacy.Private;

    // Привязка к шаблону (просто ссылка)
    public Guid? TemplateId { get; set; }
    public DocumentTemplate? Template { get; set; }

    // Создатель документа
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    // Редакторы
    public ICollection<User> Editors { get; set; } = new List<User>();

    // Привязка к организации
    public Guid? ClientOrganizationId { get; set; }
    public ClientOrganization? ClientOrganization { get; set; }

    public int CurrentVersionNumber { get; set; } = 1;

    public DateTime? SignedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}
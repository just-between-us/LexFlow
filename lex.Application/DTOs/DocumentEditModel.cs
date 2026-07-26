using Lex.Domain.Enums;

namespace lex.Application.DTOs;

public class DocumentEditDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; }
    public DocumentPrivacy Privacy { get; set; }
    public int CurrentVersionNumber { get; set; }
    public Guid? TemplateId { get; set; }
    public List<DocumentVersionSummaryDto> Versions { get; set; } = new();

    public Guid OwnerUserId { get; set; }
    public bool CurrentUserIsOwner { get; set; }
    public List<DocumentEditorDto> Editors { get; set; } = new();
    public Guid? ClientOrganizationId { get; set; }
    public string? ClientOrganizationName { get; set; }
    public bool OwnerHasOrganization { get; set; }
}

public class DocumentEditorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class DocumentVersionSummaryDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
    public string CreatedByName { get; set; } = "Неизвестный";
    public DateTime CreatedAtUtc { get; set; }
}

public class DocumentFieldsUpdateModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public DocumentPrivacy Privacy { get; set; }
    public DocumentStatus Status { get; set; }
}
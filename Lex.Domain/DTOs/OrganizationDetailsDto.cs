using Lex.Domain.Enums;

namespace Lex.Domain.DTOs;

public class OrganizationDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TaxId { get; set; }
    public string? RegistrationNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string OwnerFullName { get; set; } = string.Empty;
    public int StaffCount { get; set; }
    public bool IsCurrentUserMember { get; set; }
    public List<OrganizationDocumentDto> Documents { get; set; } = new();
    public List<OrganizationActiveChecklistDto> ActiveChecklists { get; set; } = new();
}

public class OrganizationDocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; }
    public int CurrentVersionNumber { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class OrganizationActiveChecklistDto
{
    public Guid ActiveChecklistId { get; set; }
    public Guid ChecklistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public double Progress => TotalItems > 0 ? (double)CompletedItems / TotalItems * 100 : 0;
}
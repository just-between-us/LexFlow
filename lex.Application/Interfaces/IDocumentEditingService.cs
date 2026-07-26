using lex.Application.DTOs;

namespace lex.Application.Interfaces;

public interface IDocumentEditingService
{
    Task<DocumentEditDto?> GetForEditingAsync(Guid documentId, Guid userId, CancellationToken ct = default);

    Task<DocumentEditDto> CreateNewVersionAsync(
        Guid documentId, Guid userId, int basedOnVersionNumber,
        DocumentFieldsUpdateModel fields, string content, string? changeSummary,
        CancellationToken ct = default);

    Task<DocumentEditDto> UpdateExistingVersionAsync(
        Guid documentId, Guid userId, Guid versionId,
        DocumentFieldsUpdateModel fields, string content, string? changeSummary,
        CancellationToken ct = default);

    // Новое:
    Task<DocumentEditDto> AddEditorAsync(Guid documentId, Guid requestingUserId, string emailOrUsername, CancellationToken ct = default);
    Task<DocumentEditDto> RemoveEditorAsync(Guid documentId, Guid requestingUserId, Guid editorUserId, CancellationToken ct = default);
    Task<DocumentEditDto> AttachToOwnerOrganizationAsync(Guid documentId, Guid requestingUserId, CancellationToken ct = default);
    Task<DocumentEditDto> DetachFromOrganizationAsync(Guid documentId, Guid requestingUserId, CancellationToken ct = default);
}
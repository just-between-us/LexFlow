using lex.Application.DTOs;
using Lex.Domain.Entities;
using Lex.Domain.Enums;

namespace lex.Application.Interfaces;

public interface IDocumentService
{
    Task<Document> CreateDocumentFromTemplateAsync(
        Guid templateId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TemplateForEditModel?> GetTemplateForEditingAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<Document> CreateDocumentFromTemplateAsync(
        Guid templateId,
        Guid userId,
        DocumentPrivacy privacy,
        bool archiveImmediately = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<DocumentSummaryDto> Items, int TotalCount)> GetMyDocumentsAsync(
        Guid userId, string? searchTerm, DocumentStatus? status, DocumentPrivacy? privacy,
        DocumentLifecycleFilter lifecycleFilter,
        string sortField, bool sortAscending, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    Task ToggleArchiveAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
    Task RestoreAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
}
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
}
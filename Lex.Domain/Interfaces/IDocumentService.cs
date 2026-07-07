using Lex.Domain.DTOs;
using Lex.Domain.Entities;

namespace Lex.Domain.Interfaces;

public interface IDocumentService
{
    Task<Document> CreateDocumentFromTemplateAsync(
        Guid templateId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TemplateForEditModel?> GetTemplateForEditingAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);
}
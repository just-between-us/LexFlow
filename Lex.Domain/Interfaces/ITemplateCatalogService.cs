using Lex.Application.DTOs;
using Lex.Domain.DTOs;
using Lex.Domain.Enums;

namespace Lex.Domain.Interfaces;

public interface ITemplateCatalogService
{
    Task<(IReadOnlyList<DocumentTemplateDto> Items, int TotalCount)> GetTemplatesAsync(
        string? searchTerm,
        DocumentType? type,
        int pageNumber,
        int pageSize,
        string? sortField = null,
        bool sortAscending = true,
        CancellationToken cancellationToken = default);
    
    Task<Dictionary<DocumentType, int>> GetTypeStatsAsync(CancellationToken cancellationToken = default);
}
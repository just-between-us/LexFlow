using lex.Application.DTOs;
using Lex.Domain.Enums;

namespace lex.Application.Interfaces;

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
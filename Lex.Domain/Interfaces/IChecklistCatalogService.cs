using Lex.Domain.DTOs;
using Lex.Domain.Entities;

namespace Lex.Domain.Interfaces;

public interface IChecklistCatalogService
{
    Task<(IReadOnlyList<ChecklistDto> Items, int TotalCount)> GetChecklistsAsync(
        string? searchTerm,
        List<string>? chips,
        int pageNumber,
        int pageSize,
        string? sortField = null,
        bool sortAscending = true,
        CancellationToken cancellationToken = default);
    
    Task<Dictionary<string, int>> GetChipsStatsAsync(CancellationToken cancellationToken = default);
    
    Task<Checklist?> GetChecklistWithItemsAsync(Guid checklistId, CancellationToken cancellationToken = default);
}
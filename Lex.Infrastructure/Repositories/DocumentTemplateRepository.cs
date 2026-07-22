using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Lex.Domain.Enums;
using Lex.Infrastructure.Data;

namespace Lex.Infrastructure.Repositories;

public class DocumentTemplateRepository : Repository<DocumentTemplate>
{
    public DocumentTemplateRepository(AppDbContext context) : base(context)
    {
    }
    public virtual async Task<double> GetAverageUsageCountAsync(
        CancellationToken cancellationToken = default)
    {
        var counts = await _dbSet
            .Where(t => !t.IsDeleted)
            .Select(t => _context.Documents.Count(d => !d.IsDeleted && d.TemplateId == t.Id))
            .ToListAsync(cancellationToken);

        return counts.Count == 0 ? 0 : counts.Average();
    }
    public virtual async Task<Dictionary<Guid, int>> GetUsageCountsAsync(
        IEnumerable<Guid> templateIds,
        CancellationToken cancellationToken = default)
    {
        var ids = templateIds.Distinct().ToList();
        if (!ids.Any())
            return new Dictionary<Guid, int>();

        return await _context.Documents
            .Where(d => !d.IsDeleted && d.TemplateId.HasValue && ids.Contains(d.TemplateId.Value))
            .GroupBy(d => d.TemplateId!.Value)
            .Select(g => new { TemplateId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TemplateId, x => x.Count, cancellationToken);
    }

    public virtual async Task<(IReadOnlyList<DocumentTemplate> Items, int TotalCount)> SearchTemplatesWithUsageAsync(
        string? searchTerm,
        DocumentType? type,
        int pageNumber,
        int pageSize,
        string? sortField = null,
        bool sortAscending = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(searchTerm) ||
                t.Description.ToLower().Contains(searchTerm));
        }

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        // Применяем сортировку
        IOrderedQueryable<DocumentTemplate> orderedQuery = sortField switch
        {
            "Title" => sortAscending ? query.OrderBy(t => t.Title) : query.OrderByDescending(t => t.Title),
            "Type" => sortAscending ? query.OrderBy(t => t.Type) : query.OrderByDescending(t => t.Type),
            "UsageCount" => sortAscending 
                ? query.OrderBy(t => _context.Documents.Count(d => !d.IsDeleted && d.TemplateId == t.Id))
                : query.OrderByDescending(t => _context.Documents.Count(d => !d.IsDeleted && d.TemplateId == t.Id)),
            "CreatedAtUtc" => sortAscending ? query.OrderBy(t => t.CreatedAtUtc) : query.OrderByDescending(t => t.CreatedAtUtc),
            _ => query.OrderBy(t => t.Title) // сортировка по умолчанию
        };

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                Template = t,
                UsageCount = _context.Documents.Count(d => !d.IsDeleted && d.TemplateId == t.Id)
            })
            .ToListAsync(cancellationToken);

        var templates = items.Select(x => x.Template).ToList();
        return (templates, totalCount);
    }
    
    /// Получить шаблон со всеми подсказками
    public virtual async Task<DocumentTemplate?> GetTemplateWithHintsAsync(
        Guid templateId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => !t.IsDeleted && t.Id == templateId)
            .Include(t => t.Hints.Where(h => !h.IsDeleted).OrderBy(h => h.Order))
            .FirstOrDefaultAsync(cancellationToken);
    }


    /// Получить все типы шаблонов с количеством
    public virtual async Task<Dictionary<DocumentType, int>> GetTemplateTypesStatsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => !t.IsDeleted)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);
    }
}
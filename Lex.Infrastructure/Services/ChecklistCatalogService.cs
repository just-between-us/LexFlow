using Lex.Domain.DTOs;
using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Data;

namespace Lex.Infrastructure.Services;

public class ChecklistCatalogService : IChecklistCatalogService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private static readonly TimeSpan NewThreshold = TimeSpan.FromDays(14);

    public ChecklistCatalogService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<ChecklistDto> Items, int TotalCount)> GetChecklistsAsync(
        string? searchTerm,
        List<string>? chips,
        int pageNumber,
        int pageSize,
        string? sortField = null,
        bool sortAscending = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Checklists.AsNoTracking().Where(c => !c.IsDeleted);

        // Фильтр по поиску
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.Description.ToLower().Contains(term));
        }

        // Фильтр по чипсам (теперь через связь)
        if (chips?.Any() == true)
        {
            query = query.Where(c => c.Chips.Any(cc => chips.Contains(cc.Chip)));
        }

        // Сортировка
        IQueryable<Checklist> orderedQuery = sortField?.ToLower() switch
        {
            "title" => sortAscending ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title),
            "itemscount" => sortAscending
                ? query.OrderBy(c => c.Items.Count(i => !i.IsDeleted))
                : query.OrderByDescending(c => c.Items.Count(i => !i.IsDeleted)),
            "usagecount" => sortAscending
                ? query.OrderBy(c => context.ActiveChecklists.Count(a => !a.IsDeleted && a.ChecklistId == c.Id))
                : query.OrderByDescending(c => context.ActiveChecklists.Count(a => !a.IsDeleted && a.ChecklistId == c.Id)),
            "createdatutc" => sortAscending
                ? query.OrderBy(c => c.CreatedAtUtc)
                : query.OrderByDescending(c => c.CreatedAtUtc),
            _ => sortAscending ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title)
        };

        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        var checklists = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .Include(c => c.Chips) // теперь навигационное свойство
            .ToListAsync(cancellationToken);

        var ids = checklists.Select(c => c.Id).ToList();
        var usageCounts = await GetUsageCountsAsync(ids, context, cancellationToken);
        var averageUsage = await GetAverageUsageCountAsync(context, cancellationToken);
        var now = DateTime.UtcNow;

        var dtos = checklists.Select(c =>
        {
            var usage = usageCounts.GetValueOrDefault(c.Id);
            return new ChecklistDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Chips = c.Chips.Select(cc => cc.Chip).ToList(),
                ItemsCount = c.Items.Count(i => !i.IsDeleted),
                UsageCount = usage,
                CreatedAtUtc = c.CreatedAtUtc,
                IsPopular = averageUsage > 0 && usage >= averageUsage * 1.5,
                IsNew = now - c.CreatedAtUtc <= NewThreshold
            };
        }).ToList();

        return (dtos, totalCount);
    }

    public async Task<Dictionary<string, int>> GetChipsStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ChecklistChips
            .AsNoTracking()
            .GroupBy(cc => cc.Chip)
            .Select(g => new { Chip = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Chip, x => x.Count, cancellationToken);
    }

    public async Task<Checklist?> GetChecklistWithItemsAsync(Guid checklistId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Checklists
            .AsNoTracking()
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .Include(c => c.Chips)
            .FirstOrDefaultAsync(c => c.Id == checklistId, cancellationToken);
    }

    // Вспомогательные приватные методы (не нужны публичные в интерфейсе)
    private static async Task<Dictionary<Guid, int>> GetUsageCountsAsync(
        IEnumerable<Guid> ids, AppDbContext context, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        if (!idList.Any()) return new();
        return await context.ActiveChecklists
            .Where(a => !a.IsDeleted && idList.Contains(a.ChecklistId))
            .GroupBy(a => a.ChecklistId)
            .Select(g => new { ChecklistId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ChecklistId, x => x.Count, ct);
    }

    private static async Task<double> GetAverageUsageCountAsync(AppDbContext context, CancellationToken ct)
    {
        var totalChecklists = await context.Checklists.CountAsync(c => !c.IsDeleted, ct);
        if (totalChecklists == 0) return 0;

        var totalUsages = await context.ActiveChecklists.CountAsync(a => !a.IsDeleted, ct);
        return (double)totalUsages / totalChecklists;
    }
}
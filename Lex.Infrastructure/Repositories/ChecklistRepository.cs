using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Lex.Infrastructure.Data;

namespace Lex.Infrastructure.Repositories;

public class ChecklistRepository : Repository<Checklist>
{
    public ChecklistRepository(AppDbContext context) : base(context)
    {
    }

    public virtual async Task<Checklist?> GetChecklistWithItemsAsync(Guid checklistId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => !c.IsDeleted && c.Id == checklistId)
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Checklist>> GetAllWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => !c.IsDeleted)
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Checklist>> GetPublicChecklistsAsync(CancellationToken cancellationToken = default)
    {
        // Предположим, что публичные чек-листы – это те, у которых нет привязки к организации (ClientOrganizationId == null)
        // или те, у которых есть флаг IsPublic (если добавите). Пока используем критерий: чек-лист считается публичным, если у него нет ни одного активного экземпляра в организациях? 
        // По логике, шаблоны чек-листов изначально публичны (каталог). Поэтому возвращаем все не удалённые.
        return await _dbSet
            .Where(c => !c.IsDeleted)
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Checklist>> SearchChecklistsAsync(
        string? searchTerm, 
        List<string>? chips, 
        int pageNumber = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(c => 
                c.Title.ToLower().Contains(searchTerm) ||
                c.Description.ToLower().Contains(searchTerm));
        }

        if (chips != null && chips.Any())
        {
            // Поиск по чипсам (JSON массив)
            /*
            query = query.Where(c => c.Chips.Any(chip => chips.Contains(chip)));
        */
        }

        return await query
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .OrderBy(c => c.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /*public virtual async Task<Dictionary<string, int>> GetChipsStatsAsync(CancellationToken cancellationToken = default)
    {
        var allChipsLists = await _context.Checklists
            .AsNoTracking()
            .Select(c => c.Chips)
            .ToListAsync(cancellationToken);

        return allChipsLists
            .SelectMany(chips => chips)
            .GroupBy(chip => chip)
            .ToDictionary(
                group => group.Key, 
                group => group.Count()
            );
    }*/

    public virtual async Task<bool> IsChecklistInUseAsync(Guid checklistId, CancellationToken cancellationToken = default)
    {
        return await _context.ActiveChecklists
            .AnyAsync(a => !a.IsDeleted && a.ChecklistId == checklistId, cancellationToken);
    }

    public virtual async Task<int> GetChecklistUsageCountAsync(Guid checklistId, CancellationToken cancellationToken = default)
    {
        return await _context.ActiveChecklists
            .CountAsync(a => !a.IsDeleted && a.ChecklistId == checklistId, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Checklist>> GetPopularChecklistsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => !c.IsDeleted)
            .Select(c => new
            {
                Checklist = c,
                UsageCount = _context.ActiveChecklists.Count(a => !a.IsDeleted && a.ChecklistId == c.Id)
            })
            .OrderByDescending(x => x.UsageCount)
            .Take(count)
            .Select(x => x.Checklist)
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<Checklist>> GetChecklistsByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        // Имеются в виду активные чек-листы. Для шаблонов организация не предусмотрена, возвращаем все шаблоны, которые используются в активных чек-листах этой организации.
        var checklistIds = await _context.ActiveChecklists
            .Where(a => !a.IsDeleted && a.ClientOrganizationId == organizationId)
            .Select(a => a.ChecklistId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _dbSet
            .Where(c => !c.IsDeleted && checklistIds.Contains(c.Id))
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<int> BulkDeleteChecklistsAsync(IEnumerable<Guid> checklistIds, CancellationToken cancellationToken = default)
    {
        var ids = checklistIds.ToList();
        if (!ids.Any()) return 0;

        var checklists = await _dbSet
            .Where(c => ids.Contains(c.Id) && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var c in checklists)
        {
            c.IsDeleted = true;
            c.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return checklists.Count;
    }
    public virtual async Task<(IReadOnlyList<Checklist> Items, int TotalCount)> SearchChecklistsWithUsageAsync(
        string? searchTerm,
        List<string>? chips,
        int pageNumber,
        int pageSize,
        string? sortField = null,
        bool sortAscending = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(searchTerm) ||
                c.Description.ToLower().Contains(searchTerm));
        }

        if (chips != null && chips.Any())
        {
            /*
            query = query.Where(c => c.Chips.Any(chip => chips.Contains(chip)));
        */
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Сортировка
        query = sortField?.ToLower() switch
        {
            "title" => sortAscending ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title),
            "itemscount" => sortAscending ? query.OrderBy(c => c.Items.Count(i => !i.IsDeleted)) : query.OrderByDescending(c => c.Items.Count(i => !i.IsDeleted)),
            "usagecount" => sortAscending ? query.OrderBy(c => _context.ActiveChecklists.Count(a => !a.IsDeleted && a.ChecklistId == c.Id)) : query.OrderByDescending(c => _context.ActiveChecklists.Count(a => !a.IsDeleted && a.ChecklistId == c.Id)),
            "createdatutc" => sortAscending ? query.OrderBy(c => c.CreatedAtUtc) : query.OrderByDescending(c => c.CreatedAtUtc),
            _ => sortAscending ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title)
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
    public virtual async Task<Dictionary<Guid, int>> GetUsageCountsAsync(
        IEnumerable<Guid> checklistIds,
        CancellationToken cancellationToken = default)
    {
        var ids = checklistIds.Distinct().ToList();
        if (!ids.Any())
            return new Dictionary<Guid, int>();

        return await _context.ActiveChecklists
            .Where(a => !a.IsDeleted && ids.Contains(a.ChecklistId))
            .GroupBy(a => a.ChecklistId)
            .Select(g => new { ChecklistId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ChecklistId, x => x.Count, cancellationToken);
    }
    public virtual async Task<double> GetAverageUsageCountAsync(CancellationToken cancellationToken = default)
    {
        var allIds = await _dbSet.Where(c => !c.IsDeleted).Select(c => c.Id).ToListAsync(cancellationToken);
        if (!allIds.Any())
            return 0;

        var usageCounts = await GetUsageCountsAsync(allIds, cancellationToken);
        return usageCounts.Values.DefaultIfEmpty(0).Average();
    }
}
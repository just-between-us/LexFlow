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
    public virtual async Task SaveActiveChecklistItemAsync(ActiveChecklistItem item, CancellationToken cancellationToken = default)
    {
        item.UpdatedAtUtc = DateTime.UtcNow;
        _context.ActiveChecklistItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
    }
    public virtual async Task<ActiveChecklist> GetOrCreateActiveChecklistAsync(
        Guid checklistId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ActiveChecklists
            .Include(ac => ac.Items.OrderBy(i => i.Order))
            .FirstOrDefaultAsync(ac => ac.ChecklistId == checklistId && ac.UserId == userId && !ac.IsDeleted, cancellationToken);

        if (existing != null)
            return existing;

        // Загружаем шаблон
        var template = await GetChecklistWithItemsAsync(checklistId, cancellationToken);
        if (template == null)
            throw new KeyNotFoundException("Шаблон чек-листа не найден.");

        var active = new ActiveChecklist
        {
            Id = Guid.NewGuid(),
            ChecklistId = checklistId,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            Items = template.Items.Select(i => new ActiveChecklistItem
            {
                Id = Guid.NewGuid(),
                Title = i.Title,
                Order = i.Order,
                IsCompleted = false,
                CreatedAtUtc = DateTime.UtcNow
            }).ToList()
        };

        _context.ActiveChecklists.Add(active);
        await _context.SaveChangesAsync(cancellationToken);

        return active;
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
}
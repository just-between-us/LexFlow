using Lex.Domain.Entities;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class ActiveChecklistRepository : Repository<ActiveChecklist>
{
    public ActiveChecklistRepository(AppDbContext context) : base(context) { }

    public virtual async Task<ActiveChecklist?> GetActiveChecklistWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ac => !ac.IsDeleted && ac.Id == id)
            .Include(ac => ac.Checklist)
            .Include(ac => ac.Items.OrderBy(i => i.Order))
            .Include(ac => ac.Editors)
            .Include(ac => ac.ClientOrganization)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<ActiveChecklist> GetOrCreateActiveChecklistAsync(Guid checklistId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbSet
            .Where(ac => ac.ChecklistId == checklistId && ac.UserId == userId && !ac.IsDeleted)
            .Include(ac => ac.Items.OrderBy(i => i.Order))
            .Include(ac => ac.Checklist)
            .Include(ac => ac.Editors)
            .Include(ac => ac.ClientOrganization)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
            return existing;

        var template = await _context.Checklists
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .FirstOrDefaultAsync(c => c.Id == checklistId && !c.IsDeleted, cancellationToken);
        
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

        await _dbSet.AddAsync(active, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return active;
    }

    public virtual async Task<IReadOnlyList<ActiveChecklist>> GetUserActiveChecklistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ac => !ac.IsDeleted && ac.UserId == userId)
            .Include(ac => ac.Checklist)
            .Include(ac => ac.Items.OrderBy(i => i.Order))
            .Include(ac => ac.ClientOrganization)
            .OrderByDescending(ac => ac.UpdatedAtUtc ?? ac.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task SaveActiveChecklistAsync(ActiveChecklist item, CancellationToken cancellationToken = default)
    {
        item.UpdatedAtUtc = DateTime.UtcNow;
        _context.ActiveChecklists.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
}
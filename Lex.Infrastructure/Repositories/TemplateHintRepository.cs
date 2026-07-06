using Lex.Domain.Entities;
using Lex.Domain.Enums;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class TemplateHintRepository : Repository<TemplateHint>
{
    public TemplateHintRepository(AppDbContext context) : base(context)
    {
    }

    /// Получить все подсказки для шаблона
    public virtual async Task<IReadOnlyList<TemplateHint>> GetHintsForTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(h => !h.IsDeleted && h.DocumentTemplateId == templateId)
            .OrderBy(h => h.Order)
            .ToListAsync(cancellationToken);
    }

    /// Получить подсказки по важности
    public virtual async Task<IReadOnlyList<TemplateHint>> GetHintsByImportanceAsync(
        Guid templateId,
        HintImportance importance,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(h => !h.IsDeleted && 
                   h.DocumentTemplateId == templateId && 
                   h.Importance == importance)
            .OrderBy(h => h.Order)
            .ToListAsync(cancellationToken);
    }

    /// Получить критические подсказки (Warning и Critical)
    public virtual async Task<IReadOnlyList<TemplateHint>> GetCriticalHintsAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(h => !h.IsDeleted && 
                   h.DocumentTemplateId == templateId && 
                   (h.Importance == HintImportance.Warning || 
                    h.Importance == HintImportance.Critical))
            .OrderBy(h => h.Order)
            .ToListAsync(cancellationToken);
    }

    /// Обновить порядок подсказок
    public virtual async Task UpdateHintsOrderAsync(
        Guid templateId,
        Dictionary<Guid, int> hintOrder,
        CancellationToken cancellationToken = default)
    {
        var hints = await _dbSet
            .Where(h => !h.IsDeleted && h.DocumentTemplateId == templateId)
            .ToListAsync(cancellationToken);

        foreach (var hint in hints)
        {
            if (hintOrder.TryGetValue(hint.Id, out int newOrder))
            {
                hint.Order = newOrder;
                hint.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// Переместить подсказку вверх/вниз
    public virtual async Task MoveHintAsync(
        Guid hintId,
        bool moveUp,
        CancellationToken cancellationToken = default)
    {
        var hint = await GetByIdAsync(hintId, cancellationToken)
            ?? throw new KeyNotFoundException($"Подсказка с ID {hintId} не найдена");

        var hints = await _dbSet
            .Where(h => !h.IsDeleted && 
                   h.DocumentTemplateId == hint.DocumentTemplateId)
            .OrderBy(h => h.Order)
            .ToListAsync(cancellationToken);

        var currentIndex = hints.IndexOf(hint);
        if (moveUp && currentIndex > 0)
        {
            // Меняем местами с предыдущей
            var prevHint = hints[currentIndex - 1];
            (prevHint.Order, hint.Order) = (hint.Order, prevHint.Order);
            prevHint.UpdatedAtUtc = DateTime.UtcNow;
            hint.UpdatedAtUtc = DateTime.UtcNow;
        }
        else if (!moveUp && currentIndex < hints.Count - 1)
        {
            // Меняем местами со следующей
            var nextHint = hints[currentIndex + 1];
            (nextHint.Order, hint.Order) = (hint.Order, nextHint.Order);
            nextHint.UpdatedAtUtc = DateTime.UtcNow;
            hint.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// Клонировать подсказки для нового шаблона
    public virtual async Task<IReadOnlyList<TemplateHint>> CloneHintsForTemplateAsync(
        Guid sourceTemplateId,
        Guid targetTemplateId,
        CancellationToken cancellationToken = default)
    {
        var sourceHints = await GetHintsForTemplateAsync(sourceTemplateId, cancellationToken);
        var clonedHints = new List<TemplateHint>();

        foreach (var hint in sourceHints)
        {
            var clonedHint = new TemplateHint
            {
                Id = Guid.NewGuid(),
                DocumentTemplateId = targetTemplateId,
                Text = hint.Text,
                Order = hint.Order,
                Importance = hint.Importance,
                CreatedAtUtc = DateTime.UtcNow
            };

            await AddAsync(clonedHint, cancellationToken);
            clonedHints.Add(clonedHint);
        }

        return clonedHints;
    }

    /// Получить количество подсказок по типам для шаблона
    public virtual async Task<Dictionary<HintImportance, int>> GetHintsStatsAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(h => !h.IsDeleted && h.DocumentTemplateId == templateId)
            .GroupBy(h => h.Importance)
            .Select(g => new { Importance = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Importance, x => x.Count, cancellationToken);
    }

    /// Удалить все подсказки шаблона
    public virtual async Task DeleteAllHintsForTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var hints = await _dbSet
            .Where(h => !h.IsDeleted && h.DocumentTemplateId == templateId)
            .ToListAsync(cancellationToken);

        foreach (var hint in hints)
        {
            hint.IsDeleted = true;
            hint.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// Получить подсказки с пагинацией
    public virtual async Task<(IReadOnlyList<TemplateHint> Hints, int TotalCount)> GetHintsPagedAsync(
        Guid templateId,
        int pageNumber,
        int pageSize,
        HintImportance? importance = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(h => !h.IsDeleted && h.DocumentTemplateId == templateId);

        if (importance.HasValue)
        {
            query = query.Where(h => h.Importance == importance.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var hints = await query
            .OrderBy(h => h.Order)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (hints, totalCount);
    }
}
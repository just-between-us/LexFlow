using Lex.Domain.DTOs;
using Lex.Domain.Entities;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Services;

public class ActiveChecklistService : IActiveChecklistService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ActiveChecklistService(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<ActiveChecklistDto?> GetActiveForUserAsync(Guid? userId, Guid checklistId,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Намеренно НЕ фильтруем по IsDeleted — нам важно найти
        // и удалённую запись, чтобы предложить восстановление.
        var active = await context.ActiveChecklists
            .Include(a => a.Checklist)
            .Include(a => a.Items)
            .Where(a => a.UserId == userId && a.ChecklistId == checklistId)
            .OrderByDescending(a => a.CreatedAtUtc) // берём самую свежую попытку
            .FirstOrDefaultAsync(ct);

        return active is null ? null : MapToDto(active);
    }

    public async Task<ActiveChecklistDto> StartNewAsync(Guid userId, Guid checklistId,
        Guid? clientOrganizationId = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var template = await context.Checklists
            .Include(c => c.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .FirstOrDefaultAsync(c => c.Id == checklistId && !c.IsDeleted, ct);

        if (template is null)
            throw new InvalidOperationException("Шаблон чек-листа не найден или был удалён.");
        
        var active = new ActiveChecklist
        {
            UserId = userId,
            ChecklistId = checklistId,
            ClientOrganizationId = clientOrganizationId,
            Items = template.Items.Select(i => new ActiveChecklistItem
            {
                Title = i.Title, 
                Order = i.Order,
                IsCompleted = false
            }).ToList()
        };

        context.ActiveChecklists.Add(active);
        await context.SaveChangesAsync(ct);

        active.Checklist = template;
        return MapToDto(active);
    }

    public async Task<ActiveChecklistDto> RestoreAsync(Guid activeChecklistId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists
            .Include(a => a.Checklist)
            .Include(a => a.Items)
            .FirstOrDefaultAsync(a => a.Id == activeChecklistId, ct);

        if (active is null)
            throw new InvalidOperationException("Активный чек-лист не найден.");

        active.IsDeleted = false;
        active.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var item in active.Items.Where(i => i.IsDeleted))
        {
            item.IsDeleted = false;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);
        return MapToDto(active);
    }

    private static ActiveChecklistDto MapToDto(ActiveChecklist active) => new()
    {
        Id = active.Id,
        ChecklistId = active.ChecklistId,
        ChecklistTitle = active.Checklist.Title,
        IsDeleted = active.IsDeleted,
        CreatedAtUtc = active.CreatedAtUtc,
        TotalItems = active.Items.Count(i => !i.IsDeleted),
        CompletedItems = active.Items.Count(i => !i.IsDeleted && i.IsCompleted)
    };
    
    public async Task<ActiveChecklistDetailsDto?> GetDetailsAsync(Guid activeChecklistId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists
            .Include(a => a.Checklist)
            .Include(a => a.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .FirstOrDefaultAsync(a => a.Id == activeChecklistId && !a.IsDeleted, ct);

        if (active is null) return null;

        return new ActiveChecklistDetailsDto
        {
            Id = active.Id,
            ChecklistId = active.ChecklistId,
            ChecklistTitle = active.Checklist.Title,
            ChecklistDescription = active.Checklist.Description,
            CreatedAtUtc = active.CreatedAtUtc,
            Items = active.Items.Select(i => new ActiveChecklistItemDto
            {
                Id = i.Id,
                Title = i.Title,
                Order = i.Order,
                IsCompleted = i.IsCompleted,
                Note = i.Note
            }).ToList()
        };
    }

    public async Task ToggleItemAsync(Guid activeChecklistItemId, bool isCompleted, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var item = await context.ActiveChecklistItems
            .FirstOrDefaultAsync(i => i.Id == activeChecklistItemId, ct);

        if (item is null) return;

        item.IsCompleted = isCompleted;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateNoteAsync(Guid activeChecklistItemId, string? note, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var item = await context.ActiveChecklistItems
            .FirstOrDefaultAsync(i => i.Id == activeChecklistItemId, ct);

        if (item is null) return;

        item.Note = note;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid activeChecklistId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists
            .FirstOrDefaultAsync(a => a.Id == activeChecklistId, ct);

        if (active is null) return;

        active.IsDeleted = true;
        active.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }
}
using lex.Application.DTOs;
using lex.Application.Interfaces;
using Lex.Domain.Entities;
using Lex.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace lex.Application.Services;

public class ActiveChecklistService : IActiveChecklistService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly UserManager<User> _userManager;

    public ActiveChecklistService(IDbContextFactory<AppDbContext> contextFactory, UserManager<User> userManager)
    {
        _contextFactory = contextFactory;
        _userManager = userManager;
    }

    public async Task<ActiveChecklistDto?> GetActiveForUserAsync(Guid userId, Guid checklistId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists
            .Include(a => a.Checklist)
            .Include(a => a.Items)
            .Where(a => a.UserId == userId && a.ChecklistId == checklistId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        return active is null ? null : MapToSummaryDto(active);
    }

    public async Task<ActiveChecklistDto> StartNewAsync(Guid userId, Guid checklistId, Guid? clientOrganizationId = null, CancellationToken ct = default)
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
        return MapToSummaryDto(active);
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
        return MapToSummaryDto(active);
    }

    public async Task<ActiveChecklistDetailsDto?> GetDetailsAsync(Guid activeChecklistId, Guid requestingUserId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists
            .Include(a => a.Checklist)
            .Include(a => a.User)
            .Include(a => a.Editors)
            .Include(a => a.ClientOrganization)
            .Include(a => a.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .FirstOrDefaultAsync(a => a.Id == activeChecklistId && !a.IsDeleted, ct);

        if (active is null) return null;

        var isOwner = active.UserId == requestingUserId;
        var isEditor = active.Editors.Any(e => e.Id == requestingUserId);

        if (!isOwner && !isEditor)
            throw new UnauthorizedAccessException("У вас нет доступа к этому чек-листу.");

        return MapToDetailsDto(active, isOwner, isEditor);
    }

    public async Task ToggleItemAsync(Guid activeChecklistItemId, Guid requestingUserId, bool isCompleted, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var item = await context.ActiveChecklistItems
            .Include(i => i.ActiveChecklist).ThenInclude(a => a.Editors)
            .FirstOrDefaultAsync(i => i.Id == activeChecklistItemId, ct);

        if (item is null) return;
        EnsureCanEdit(item.ActiveChecklist, requestingUserId);

        item.IsCompleted = isCompleted;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateNoteAsync(Guid activeChecklistItemId, Guid requestingUserId, string? note, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var item = await context.ActiveChecklistItems
            .Include(i => i.ActiveChecklist).ThenInclude(a => a.Editors)
            .FirstOrDefaultAsync(i => i.Id == activeChecklistItemId, ct);

        if (item is null) return;
        EnsureCanEdit(item.ActiveChecklist, requestingUserId);

        item.Note = note;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid activeChecklistId, Guid requestingUserId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists.FirstOrDefaultAsync(a => a.Id == activeChecklistId, ct);
        if (active is null) return;

        if (active.UserId != requestingUserId)
            throw new UnauthorizedAccessException("Удалить прогресс может только создатель чек-листа.");

        active.IsDeleted = true;
        active.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task<ActiveChecklistDetailsDto> AddEditorAsync(Guid activeChecklistId, Guid requestingUserId, string emailOrUsername, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists
            .Include(a => a.Editors)
            .FirstOrDefaultAsync(a => a.Id == activeChecklistId && !a.IsDeleted, ct);

        if (active is null) throw new KeyNotFoundException("Чек-лист не найден.");
        if (active.UserId != requestingUserId) throw new UnauthorizedAccessException("Только создатель может добавлять редакторов.");

        emailOrUsername = emailOrUsername.Trim();
        var target = await _userManager.FindByEmailAsync(emailOrUsername)
                     ?? await _userManager.FindByNameAsync(emailOrUsername);

        if (target is null) throw new InvalidOperationException("Пользователь с таким email или логином не найден.");
        if (target.Id == active.UserId) throw new InvalidOperationException("Создатель уже имеет полный доступ.");
        if (active.Editors.Any(e => e.Id == target.Id)) throw new InvalidOperationException("Этот пользователь уже добавлен как редактор.");

        active.Editors.Add(target);
        active.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return await GetDetailsAsync(activeChecklistId, requestingUserId, ct)
               ?? throw new InvalidOperationException("Не удалось загрузить чек-лист после добавления редактора.");
    }

    public async Task<ActiveChecklistDetailsDto> RemoveEditorAsync(Guid activeChecklistId, Guid requestingUserId, Guid editorUserId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists
            .Include(a => a.Editors)
            .FirstOrDefaultAsync(a => a.Id == activeChecklistId && !a.IsDeleted, ct);

        if (active is null) throw new KeyNotFoundException("Чек-лист не найден.");
        if (active.UserId != requestingUserId) throw new UnauthorizedAccessException("Только создатель может удалять редакторов.");

        var editor = active.Editors.FirstOrDefault(e => e.Id == editorUserId);
        if (editor is not null)
        {
            active.Editors.Remove(editor);
            active.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }

        return await GetDetailsAsync(activeChecklistId, requestingUserId, ct)
               ?? throw new InvalidOperationException("Не удалось загрузить чек-лист после удаления редактора.");
    }

    public async Task<ActiveChecklistDetailsDto> AttachToOwnerOrganizationAsync(Guid activeChecklistId, Guid requestingUserId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists.FirstOrDefaultAsync(a => a.Id == activeChecklistId && !a.IsDeleted, ct);
        if (active is null) throw new KeyNotFoundException("Чек-лист не найден.");
        if (active.UserId != requestingUserId) throw new UnauthorizedAccessException("Только создатель может привязать чек-лист к организации.");

        var owner = await context.Users.FirstOrDefaultAsync(u => u.Id == requestingUserId, ct);
        if (owner?.ClientOrganizationId is null)
            throw new InvalidOperationException("У вас нет организации.");

        active.ClientOrganizationId = owner.ClientOrganizationId;
        active.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return await GetDetailsAsync(activeChecklistId, requestingUserId, ct)
               ?? throw new InvalidOperationException("Не удалось загрузить чек-лист после привязки.");
    }

    public async Task<ActiveChecklistDetailsDto> DetachFromOrganizationAsync(Guid activeChecklistId, Guid requestingUserId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var active = await context.ActiveChecklists.FirstOrDefaultAsync(a => a.Id == activeChecklistId && !a.IsDeleted, ct);
        if (active is null) throw new KeyNotFoundException("Чек-лист не найден.");
        if (active.UserId != requestingUserId) throw new UnauthorizedAccessException("Только создатель может отвязать чек-лист от организации.");

        active.ClientOrganizationId = null;
        active.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return await GetDetailsAsync(activeChecklistId, requestingUserId, ct)
               ?? throw new InvalidOperationException("Не удалось загрузить чек-лист после отвязки.");
    }

    private static void EnsureCanEdit(ActiveChecklist active, Guid userId)
    {
        var canEdit = active.UserId == userId || active.Editors.Any(e => e.Id == userId);
        if (!canEdit) throw new UnauthorizedAccessException("У вас нет прав на редактирование этого чек-листа.");
    }

    private static ActiveChecklistDto MapToSummaryDto(ActiveChecklist active) => new()
    {
        Id = active.Id,
        ChecklistId = active.ChecklistId,
        ChecklistTitle = active.Checklist.Title,
        IsDeleted = active.IsDeleted,
        CreatedAtUtc = active.CreatedAtUtc,
        TotalItems = active.Items.Count(i => !i.IsDeleted),
        CompletedItems = active.Items.Count(i => !i.IsDeleted && i.IsCompleted)
    };

    private static ActiveChecklistDetailsDto MapToDetailsDto(ActiveChecklist active, bool isOwner, bool isEditor) => new()
    {
        Id = active.Id,
        ChecklistId = active.ChecklistId,
        ChecklistTitle = active.Checklist.Title,
        ChecklistDescription = active.Checklist.Description,
        CreatedAtUtc = active.CreatedAtUtc,
        Items = active.Items.Select(i => new ActiveChecklistItemDto
        {
            Id = i.Id, Title = i.Title, Order = i.Order, IsCompleted = i.IsCompleted, Note = i.Note
        }).ToList(),
        OwnerUserId = active.UserId,
        OwnerFullName = string.IsNullOrWhiteSpace(active.User.GetFullName()) ? (active.User.Email ?? "—") : active.User.GetFullName()!,
        CurrentUserIsOwner = isOwner,
        CurrentUserCanEdit = isOwner || isEditor,
        Editors = active.Editors.Select(e => new ActiveChecklistEditorDto
        {
            UserId = e.Id,
            FullName = string.IsNullOrWhiteSpace(e.GetFullName()) ? (e.Email ?? "Без имени") : e.GetFullName()!,
            Email = e.Email
        }).OrderBy(e => e.FullName).ToList(),
        ClientOrganizationId = active.ClientOrganizationId,
        ClientOrganizationName = active.ClientOrganization?.Name,
        OwnerHasOrganization = active.User.ClientOrganizationId.HasValue
    };
}
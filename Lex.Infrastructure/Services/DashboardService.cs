using Lex.Domain.DTOs;
using Lex.Domain.Entities;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private const int RecentItemsCount = 5;

    public DashboardService(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<PersonalDashboardDto> GetPersonalDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var checklists = await context.ActiveChecklists
            .AsNoTracking()
            .Include(a => a.Items)
            .Include(a => a.Checklist)
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

        var documents = await context.Documents
            .AsNoTracking()
            .Where(d => d.CreatedByUserId == userId || d.Editors.Any(e => e.Id == userId))
            .ToListAsync(ct);

        return new PersonalDashboardDto
        {
            Checklists = BuildChecklistStats(checklists),
            Documents = BuildDocumentStats(documents),
            RecentChecklists = checklists
                .OrderByDescending(a => a.UpdatedAtUtc ?? a.CreatedAtUtc)
                .Take(RecentItemsCount)
                .Select(a => MapChecklist(a, ownerName: null))
                .ToList(),
            RecentDocuments = documents
                .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
                .Take(RecentItemsCount)
                .Select(d => MapDocument(d, ownerName: null))
                .ToList()
        };
    }

    public async Task<OrganizationDashboardDto?> GetOrganizationDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var org = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.ClientOrganizationId)
            .FirstOrDefaultAsync(ct);

        if (org is null) return null;

        var organization = await context.ClientOrganizations
            .AsNoTracking()
            .Include(o => o.Staff)
            .Include(o => o.OwnerUser)
            .FirstOrDefaultAsync(o => o.Id == org, ct);

        if (organization is null) return null;

        var checklists = await context.ActiveChecklists
            .AsNoTracking()
            .Include(a => a.Items)
            .Include(a => a.User)
            .Include(a => a.Checklist)
            .Where(a => a.ClientOrganizationId == org)
            .ToListAsync(ct);

        var documents = await context.Documents
            .AsNoTracking()
            .Include(d => d.CreatedByUser)
            .Where(d => d.ClientOrganizationId == org)
            .ToListAsync(ct);

        var contributors = checklists
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, ChecklistsStarted = g.Count() })
            .ToList();

        var docCreators = documents
            .GroupBy(d => d.CreatedByUserId)
            .Select(g => new { UserId = g.Key, DocumentsCreated = g.Count() })
            .ToList();

        var allStaff = organization.Staff.Append(organization.OwnerUser).DistinctBy(u => u.Id).ToList();

        var topContributors = allStaff
            .Select(u => new StaffActivityDto
            {
                UserId = u.Id,
                FullName = string.IsNullOrWhiteSpace(u.GetFullName()) ? (u.Email ?? "Без имени") : u.GetFullName()!,
                ChecklistsStarted = contributors.FirstOrDefault(c => c.UserId == u.Id)?.ChecklistsStarted ?? 0,
                DocumentsCreated = docCreators.FirstOrDefault(c => c.UserId == u.Id)?.DocumentsCreated ?? 0
            })
            .Where(s => s.ChecklistsStarted > 0 || s.DocumentsCreated > 0)
            .OrderByDescending(s => s.ChecklistsStarted + s.DocumentsCreated)
            .Take(RecentItemsCount)
            .ToList();

        return new OrganizationDashboardDto
        {
            OrganizationName = organization.Name,
            StaffCount = allStaff.Count,
            Checklists = BuildChecklistStats(checklists),
            Documents = BuildDocumentStats(documents),
            RecentChecklists = checklists
                .OrderByDescending(a => a.UpdatedAtUtc ?? a.CreatedAtUtc)
                .Take(RecentItemsCount)
                .Select(a => MapChecklist(a, a.User.GetFullName()))
                .ToList(),
            RecentDocuments = documents
                .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
                .Take(RecentItemsCount)
                .Select(d => MapDocument(d, d.CreatedByUser.GetFullName()))
                .ToList(),
            TopContributors = topContributors
        };
    }

    private static ChecklistDashboardStatsDto BuildChecklistStats(List<ActiveChecklist> checklists)
    {
        var total = checklists.Count;
        var completed = checklists.Count(a => a.Items.Count > 0 && a.Items.All(i => i.IsCompleted));
        return new ChecklistDashboardStatsDto
        {
            TotalActive = total,
            Completed = completed,
            InProgress = total - completed,
            CompletionRatePercent = total > 0 ? Math.Round(100.0 * completed / total, 0) : 0
        };
    }

    private static DocumentDashboardStatsDto BuildDocumentStats(List<Document> documents)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        return new DocumentDashboardStatsDto
        {
            Total = documents.Count,
            ByStatus = documents.GroupBy(d => d.Status).ToDictionary(g => g.Key, g => g.Count()),
            EditedLast7Days = documents.Count(d => (d.UpdatedAtUtc ?? d.CreatedAtUtc) >= weekAgo)
        };
    }

    private static ChecklistActivitySummaryDto MapChecklist(ActiveChecklist a, string? ownerName) => new()
    {
        Id = a.Id,
        ChecklistTitle = a.Checklist?.Title ?? "Без названия",
        ProgressPercent = a.Items.Count > 0
            ? Math.Round(100.0 * a.Items.Count(i => i.IsCompleted) / a.Items.Count, 0)
            : 0,
        UpdatedAtUtc = a.UpdatedAtUtc ?? a.CreatedAtUtc,
        OwnerName = ownerName
    };

    private static DocumentActivitySummaryDto MapDocument(Document d, string? ownerName) => new()
    {
        Id = d.Id,
        Title = d.Title,
        Status = d.Status,
        UpdatedAtUtc = d.UpdatedAtUtc ?? d.CreatedAtUtc,
        OwnerName = ownerName
    };
    
    public async Task<ActivityDashboardDto> GetActivityAsync(Guid userId, bool organizationScope, CancellationToken ct = default)
{
    await using var context = await _contextFactory.CreateDbContextAsync(ct);

    Guid? organizationId = null;
    if (organizationScope)
    {
        organizationId = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.ClientOrganizationId)
            .FirstOrDefaultAsync(ct);

        if (organizationId is null)
            organizationScope = false; // нет организации — молча падаем в личный скоуп
    }

    const int rangeDays = 371; // 53 недели, как у GitHub
    var rangeStartUtc = DateTime.UtcNow.Date.AddDays(-(rangeDays - 1));

    var checklists = await context.ActiveChecklists
        .AsNoTracking()
        .Include(a => a.Checklist)
        .Include(a => a.Items)
        .Include(a => a.User)
        .Where(a => organizationScope ? a.ClientOrganizationId == organizationId : a.UserId == userId)
        .ToListAsync(ct);

    var documents = await context.Documents
        .AsNoTracking()
        .Include(d => d.CreatedByUser)
        .Include(d => d.Versions).ThenInclude(v => v.VersionCreatedByUser)
        .Where(d => organizationScope
            ? d.ClientOrganizationId == organizationId
            : (d.CreatedByUserId == userId || d.Editors.Any(e => e.Id == userId)))
        .ToListAsync(ct);

    var events = new List<ActivityEventDto>();

    foreach (var a in checklists)
    {
        if (a.CreatedAtUtc >= rangeStartUtc)
        {
            events.Add(new ActivityEventDto
            {
                OccurredAtUtc = a.CreatedAtUtc,
                Type = ActivityEventType.ChecklistStarted,
                Title = $"Начат чек-лист «{a.Checklist?.Title ?? "Без названия"}»",
                NavigateUrl = $"/checklists/active/{a.Id}",
                ActorName = organizationScope ? (a.User.GetFullName() ?? a.User.Email) : null
            });
        }

        foreach (var item in a.Items)
        {
            // UpdatedAtUtc проставляется только при реальном изменении (тоггл/заметка),
            // так что момент старта чек-листа (когда пункты создаются) не задваивает событие
            if (item.UpdatedAtUtc is { } updatedAt && updatedAt >= rangeStartUtc)
            {
                events.Add(new ActivityEventDto
                {
                    OccurredAtUtc = updatedAt,
                    Type = item.IsCompleted ? ActivityEventType.ChecklistItemCompleted : ActivityEventType.ChecklistItemUpdated,
                    Title = item.IsCompleted ? $"Отмечен пункт «{item.Title}»" : $"Обновлён пункт «{item.Title}»",
                    SubTitle = a.Checklist?.Title,
                    NavigateUrl = $"/checklists/active/{a.Id}",
                    ActorName = organizationScope ? (a.User.GetFullName() ?? a.User.Email) : null
                });
            }
        }
    }

    foreach (var d in documents)
    {
        if (d.CreatedAtUtc >= rangeStartUtc)
        {
            events.Add(new ActivityEventDto
            {
                OccurredAtUtc = d.CreatedAtUtc,
                Type = ActivityEventType.DocumentCreated,
                Title = $"Создан документ «{d.Title}»",
                NavigateUrl = $"/documents/{d.Id}",
                ActorName = organizationScope ? (d.CreatedByUser.GetFullName() ?? d.CreatedByUser.Email) : null
            });
        }

        foreach (var v in d.Versions)
        {
            if (v.CreatedAtUtc >= rangeStartUtc)
            {
                events.Add(new ActivityEventDto
                {
                    OccurredAtUtc = v.CreatedAtUtc,
                    Type = ActivityEventType.DocumentVersionSaved,
                    Title = $"Сохранена версия v{v.VersionNumber} документа «{d.Title}»",
                    SubTitle = v.ChangeSummary,
                    NavigateUrl = $"/documents/{d.Id}",
                    ActorName = organizationScope ? (v.VersionCreatedByUser?.GetFullName() ?? v.VersionCreatedByUser?.Email) : null
                });
            }
        }
    }

    var countsByDate = events
        .GroupBy(e => DateOnly.FromDateTime(e.OccurredAtUtc.ToLocalTime()))
        .ToDictionary(g => g.Key, g => g.Count());

    var startDate = DateOnly.FromDateTime(rangeStartUtc.ToLocalTime());
    var today = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime());

    var heatmapDays = new List<ActivityHeatmapDayDto>();
    for (var day = startDate; day <= today; day = day.AddDays(1))
    {
        heatmapDays.Add(new ActivityHeatmapDayDto { Date = day, Count = countsByDate.GetValueOrDefault(day) });
    }

    var streak = 0;
    var cursor = today;
    while (countsByDate.TryGetValue(cursor, out var c) && c > 0)
    {
        streak++;
        cursor = cursor.AddDays(-1);
    }

    return new ActivityDashboardDto
    {
        HeatmapDays = heatmapDays,
        TotalEventsInRange = events.Count,
        CurrentStreakDays = streak,
        RecentEvents = events.OrderByDescending(e => e.OccurredAtUtc).Take(30).ToList()
    };
}
}
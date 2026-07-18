namespace Lex.Domain.DTOs;

public enum ActivityEventType
{
    ChecklistStarted,
    ChecklistItemCompleted,
    ChecklistItemUpdated,
    DocumentCreated,
    DocumentVersionSaved
}

public class ActivityEventDto
{
    public DateTime OccurredAtUtc { get; set; }
    public ActivityEventType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? SubTitle { get; set; }
    public string? NavigateUrl { get; set; }
    public string? ActorName { get; set; } // только для org-скоупа
}

public class ActivityHeatmapDayDto
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public class ActivityDashboardDto
{
    public List<ActivityHeatmapDayDto> HeatmapDays { get; set; } = new(); // от старой к новой дате
    public int TotalEventsInRange { get; set; }
    public int CurrentStreakDays { get; set; }
    public List<ActivityEventDto> RecentEvents { get; set; } = new();
}
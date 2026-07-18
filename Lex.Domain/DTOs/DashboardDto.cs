using Lex.Domain.Enums;

namespace Lex.Domain.DTOs;

public class ChecklistDashboardStatsDto
{
    public int TotalActive { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public double CompletionRatePercent { get; set; }
}

public class DocumentDashboardStatsDto
{
    public int Total { get; set; }
    public Dictionary<DocumentStatus, int> ByStatus { get; set; } = new();
    public int EditedLast7Days { get; set; }
}

public class ChecklistActivitySummaryDto
{
    public Guid Id { get; set; }
    public string ChecklistTitle { get; set; } = string.Empty;
    public double ProgressPercent { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? OwnerName { get; set; } // заполняется только для организации
}

public class DocumentActivitySummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? OwnerName { get; set; }
}

public class StaffActivityDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int ChecklistsStarted { get; set; }
    public int DocumentsCreated { get; set; }
}

public class PersonalDashboardDto
{
    public ChecklistDashboardStatsDto Checklists { get; set; } = new();
    public DocumentDashboardStatsDto Documents { get; set; } = new();
    public List<ChecklistActivitySummaryDto> RecentChecklists { get; set; } = new();
    public List<DocumentActivitySummaryDto> RecentDocuments { get; set; } = new();
}

public class OrganizationDashboardDto
{
    public string OrganizationName { get; set; } = string.Empty;
    public int StaffCount { get; set; }
    public ChecklistDashboardStatsDto Checklists { get; set; } = new();
    public DocumentDashboardStatsDto Documents { get; set; } = new();
    public List<ChecklistActivitySummaryDto> RecentChecklists { get; set; } = new();
    public List<DocumentActivitySummaryDto> RecentDocuments { get; set; } = new();
    public List<StaffActivityDto> TopContributors { get; set; } = new();
}
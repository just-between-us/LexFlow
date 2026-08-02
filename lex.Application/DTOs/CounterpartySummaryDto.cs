using Lex.Domain.Enums;

namespace lex.Application.DTOs;

public class CounterpartySummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserActivityType ActivityType { get; set; }
    public string? JobTitle { get; set; }
    public string? Region { get; set; }
    public string? OrganizationName { get; set; } // связанная ClientOrganization ИЛИ свободный текст из профиля
    public bool IsVerifiedOrganization { get; set; } // true, если это реальная зарегистрированная организация, а не текст
    public DateTime MemberSinceUtc { get; set; }
}

public class CounterpartySearchResultDto
{
    public IReadOnlyList<CounterpartySummaryDto> Items { get; set; } = new List<CounterpartySummaryDto>();
    public int TotalCount { get; set; }
}
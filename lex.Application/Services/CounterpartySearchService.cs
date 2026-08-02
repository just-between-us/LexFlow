using lex.Application.DTOs;
using lex.Application.Interfaces;
using Lex.Domain.Entities;
using Lex.Domain.Enums;
using Lex.Infrastructure.Repositories;

namespace lex.Application.Services;

public class CounterpartySearchService : ICounterpartySearchService
{
    private readonly CounterpartyRepository _repository;

    public CounterpartySearchService(CounterpartyRepository repository)
    {
        _repository = repository;
    }

    public async Task<CounterpartySearchResultDto> SearchAsync(
        Guid currentUserId,
        string? searchTerm,
        UserActivityType? activityType,
        string sortField,
        bool sortAscending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _repository.SearchPublicCounterpartiesAsync(
            currentUserId, searchTerm, activityType, sortField, sortAscending, pageNumber, pageSize, cancellationToken);

        return new CounterpartySearchResultDto
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total
        };
    }

    private static CounterpartySummaryDto MapToDto(User u)
    {
        var hasOrganization = u.ClientOrganization is not null;

        return new CounterpartySummaryDto
        {
            Id = u.Id,
            FullName = string.IsNullOrWhiteSpace(u.GetFullName()) ? "Без имени" : u.GetFullName()!,
            ActivityType = u.ActivityType,
            JobTitle = u.Profile?.JobTitle,
            Region = u.Profile?.Region,
            OrganizationName = hasOrganization ? u.ClientOrganization!.Name : u.Profile?.CompanyName,
            IsVerifiedOrganization = hasOrganization,
            MemberSinceUtc = u.CreatedAtUtc
        };
    }
}
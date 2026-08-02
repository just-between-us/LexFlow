using lex.Application.DTOs;
using Lex.Domain.Enums;

namespace lex.Application.Interfaces;

public interface ICounterpartySearchService
{
    Task<CounterpartySearchResultDto> SearchAsync(
        Guid currentUserId,
        string? searchTerm,
        UserActivityType? activityType,
        string sortField,
        bool sortAscending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
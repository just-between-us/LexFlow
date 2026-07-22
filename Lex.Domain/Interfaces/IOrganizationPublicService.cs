using Lex.Domain.DTOs;

namespace Lex.Domain.Interfaces;

public interface IOrganizationPublicService
{
    Task<(IReadOnlyList<PublicOrganizationListItemDto> Items, int TotalCount)> GetPublicOrganizationsAsync(
        string? searchTerm,
        string? sortField,
        bool sortAscending,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);
    Task<OrganizationDetailsDto?> GetOrganizationDetailsAsync(Guid organizationId, Guid currentUserId, CancellationToken ct = default);
}
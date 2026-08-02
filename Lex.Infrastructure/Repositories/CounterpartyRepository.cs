using Lex.Domain.Entities;
using Lex.Domain.Enums;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class CounterpartyRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CounterpartyRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> SearchPublicCounterpartiesAsync(
        Guid excludeUserId,
        string? searchTerm,
        UserActivityType? activityType,
        string? sortField,
        bool sortAscending,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var query = context.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .Include(u => u.ClientOrganization)
            .Where(u => u.Privacy == UserPrivacy.Public && u.IsActive && u.Id != excludeUserId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
                (u.Profile != null && u.Profile.CompanyName != null && u.Profile.CompanyName.ToLower().Contains(term)) ||
                (u.Profile != null && u.Profile.JobTitle != null && u.Profile.JobTitle.ToLower().Contains(term)) ||
                (u.ClientOrganization != null && u.ClientOrganization.Name.ToLower().Contains(term)));
        }

        if (activityType.HasValue)
            query = query.Where(u => u.ActivityType == activityType.Value);

        var totalCount = await query.CountAsync(ct);

        IOrderedQueryable<User> orderedQuery = sortField?.ToLower() switch
        {
            "recent" => sortAscending ? query.OrderBy(u => u.CreatedAtUtc) : query.OrderByDescending(u => u.CreatedAtUtc),
            _ => sortAscending 
                ? query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                : query.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName)
        };

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
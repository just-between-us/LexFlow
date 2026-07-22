using Lex.Domain.Entities;
using Lex.Domain.Enums;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class ClientOrganizationRepository : Repository<ClientOrganization>
{
    public ClientOrganizationRepository(AppDbContext context) : base(context) { }

    public async Task<ClientOrganization?> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(o => o.OwnerUser)
            .Include(o => o.Staff)
            .Where(o => !o.IsDeleted && (o.OwnerUserId == userId || o.Staff.Any(u => u.Id == userId)))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ClientOrganization?> GetByIdWithStaffAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(o => o.OwnerUser)
            .Include(o => o.Staff)
            .FirstOrDefaultAsync(o => o.Id == organizationId && !o.IsDeleted, ct);
    }
    public async Task<(IReadOnlyList<ClientOrganization> Items, int TotalCount)> GetPublicOrganizationsAsync(
        string? searchTerm,
        string? sortField,
        bool sortAscending,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(o => o.OwnerUser)
            .Include(o => o.Staff)
            .Where(o => !o.IsDeleted && o.Privacy == OrganizationPrivacy.Public && o.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(o =>
                o.Name.ToLower().Contains(searchTerm) ||
                (o.Description != null && o.Description.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(ct);

        // Сортировка
        IOrderedQueryable<ClientOrganization> orderedQuery = sortField switch
        {
            "Name" => sortAscending ? query.OrderBy(o => o.Name) : query.OrderByDescending(o => o.Name),
            "StaffCount" => sortAscending
                ? query.OrderBy(o => o.Staff.Count(u => u.ClientOrganizationId == o.Id))
                : query.OrderByDescending(o => o.Staff.Count(u => u.ClientOrganizationId == o.Id)),
            "CreatedAtUtc" => sortAscending
                ? query.OrderBy(o => o.CreatedAtUtc)
                : query.OrderByDescending(o => o.CreatedAtUtc),
            _ => query.OrderBy(o => o.Name) // по умолчанию по названию
        };

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
    // В ClientOrganizationRepository.cs

    public async Task<ClientOrganization?> GetOrganizationDetailsAsync(Guid orgId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(o => o.OwnerUser)
            .Include(o => o.Staff)
            .FirstOrDefaultAsync(o => o.Id == orgId && !o.IsDeleted && o.Privacy == OrganizationPrivacy.Public && o.IsActive, ct);
    }

    public async Task<IReadOnlyList<Document>> GetPublicDocumentsByOrganizationAsync(Guid orgId, CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => !d.IsDeleted && d.ClientOrganizationId == orgId && d.Privacy == DocumentPrivacy.Public)
            .Include(d => d.CreatedByUser)
            .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ActiveChecklist>> GetActiveChecklistsByOrganizationAsync(Guid orgId, CancellationToken ct = default)
    {
        return await _context.ActiveChecklists
            .Include(ac => ac.Checklist)
            .Include(ac => ac.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            .Where(ac => !ac.IsDeleted && ac.ClientOrganizationId == orgId)
            .OrderByDescending(ac => ac.UpdatedAtUtc ?? ac.CreatedAtUtc)
            .ToListAsync(ct);
    }
}
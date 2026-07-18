using Lex.Domain.Entities;
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
}
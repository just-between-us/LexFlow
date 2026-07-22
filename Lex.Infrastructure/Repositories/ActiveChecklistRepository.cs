using Lex.Domain.Entities;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class ActiveChecklistRepository : Repository<ActiveChecklist>
{
    public ActiveChecklistRepository(AppDbContext context) : base(context) { }

    public virtual async Task<IReadOnlyList<ActiveChecklist>> GetUserActiveChecklistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ac => !ac.IsDeleted && ac.UserId == userId)
            .Include(ac => ac.Checklist)
            .Include(ac => ac.Items.OrderBy(i => i.Order))
            .Include(ac => ac.ClientOrganization)
            .OrderByDescending(ac => ac.UpdatedAtUtc ?? ac.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
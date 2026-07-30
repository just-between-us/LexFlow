using Lex.Domain.Entities;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class ActiveChecklistRepository : Repository<ActiveChecklist>
{
    public ActiveChecklistRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

    public virtual async Task<IReadOnlyList<ActiveChecklist>> GetUserActiveChecklistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.ActiveChecklists
            .Where(ac => !ac.IsDeleted && ac.UserId == userId)
            .Include(ac => ac.Checklist)
            .Include(ac => ac.Items.OrderBy(i => i.Order))
            .Include(ac => ac.ClientOrganization)
            .OrderByDescending(ac => ac.UpdatedAtUtc ?? ac.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
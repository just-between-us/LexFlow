using Lex.Domain.Entities;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class UserProfileRepository : Repository<UserProfile>
{
    public UserProfileRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory)
    {
    }

    public async Task AddNewUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.UserProfiles.Add(userProfile);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.UserProfiles.Update(userProfile);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfile?> GetProfileWithUserAsync(Guid userId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.UserProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, ct);
    }
}
using Lex.Domain.Entities;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class UserProfileRepository(AppDbContext context) : Repository<UserProfile>(context)
{
    public async Task AddNewUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        _dbSet.Add(userProfile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(userProfile);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<UserProfile?> GetProfileWithUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, ct);
    }
}
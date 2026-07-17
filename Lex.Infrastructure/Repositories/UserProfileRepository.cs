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

    public async Task DeleteUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(userProfile);
        await _context.SaveChangesAsync(cancellationToken);
    }
    public async Task<UserProfile?> GetProfileWithUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, ct);
    }

    public async Task<IReadOnlyList<UserProfile>> SearchProfilesAsync(string? search, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = _dbSet.Include(p => p.User).Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p =>
                p.User.FirstName.ToLower().Contains(term) ||
                p.User.LastName.ToLower().Contains(term) ||
                p.CompanyName.ToLower().Contains(term) ||
                p.JobTitle.ToLower().Contains(term));
        }
        return await query
            .OrderBy(p => p.User.LastName)
            .ThenBy(p => p.User.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
    
    
}
using Lex.Domain.DTOs;
using Lex.Domain.Entities;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _dbContext;

    public UserManagementService(UserManager<User> userManager, AppDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<List<UserWithRoles>> GetUsersAsync(string? searchTerm, bool? isActive, bool? isLocked)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(term) ||
                                     u.FirstName.ToLower().Contains(term) ||
                                     u.LastName.ToLower().Contains(term));
        }
        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);
        if (isLocked.HasValue)
            query = query.Where(u => isLocked.Value ? u.LockoutEnd != null : u.LockoutEnd == null);

        var users = await query.OrderBy(u => u.CreatedAtUtc).ToListAsync();
        var result = new List<UserWithRoles>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserWithRoles { User = user, Roles = roles.ToList() });
        }
        return result;
    }

    public async Task UpdateUserAsync(User user, string firstName, string lastName, bool isActive, Dictionary<string, bool> roles)
    {
        user.FirstName = firstName;
        user.LastName = lastName;
        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);

        var allRoles = await GetAllRoleNamesAsync();
        foreach (var role in allRoles)
        {
            bool hasRole = await _userManager.IsInRoleAsync(user, role);
            bool shouldHaveRole = roles.ContainsKey(role) && roles[role];
            if (shouldHaveRole && !hasRole)
                await _userManager.AddToRoleAsync(user, role);
            else if (!shouldHaveRole && hasRole)
                await _userManager.RemoveFromRoleAsync(user, role);
        }
    }

    public async Task ToggleUserLockAsync(User user)
    {
        if (await _userManager.IsLockedOutAsync(user))
            await _userManager.SetLockoutEndDateAsync(user, null);
        else
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
    }

    public async Task SoftDeleteUserAsync(User user)
    {
        user.IsActive = false;
        await _userManager.UpdateAsync(user);
    }

    public async Task<List<string>> GetAllRoleNamesAsync()
    {
        return await _dbContext.Roles.Select(r => r.Name).ToListAsync();
    }
}
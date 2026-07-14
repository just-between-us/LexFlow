using Lex.Domain.DTOs;
using Lex.Domain.Entities;

namespace Lex.Domain.Interfaces;

public interface IUserManagementService
{
    Task<List<UserWithRoles>> GetUsersAsync(string? searchTerm, bool? isActive, bool? isLocked);
    Task UpdateUserAsync(User user, string firstName, string lastName, bool isActive, Dictionary<string, bool> roles);
    Task ToggleUserLockAsync(User user);
    Task SoftDeleteUserAsync(User user);
    Task<List<string>> GetAllRoleNamesAsync();
}